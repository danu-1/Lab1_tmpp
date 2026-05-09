using DentalClinic.Entities;
using DentalClinic.Enums;
using DentalClinic.Interfaces;
using DentalClinic.Repositories;
using DentalClinic.Services;

namespace DentalClinic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║    SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ      ║");
            Console.WriteLine("║              Laborator 1 – Principii OOP + SOLID     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");

            // ── 1. Inițializare repository-uri (DIP) ───────────────────
            IRepository<Patient>         patientRepo     = new InMemoryRepository<Patient>();
            IRepository<Doctor>          doctorRepo      = new InMemoryRepository<Doctor>();
            IRepository<Appointment>     appointmentRepo = new InMemoryRepository<Appointment>();
            IRepository<TreatmentRecord> treatmentRepo   = new InMemoryRepository<TreatmentRecord>();
            IRepository<Payment>         paymentRepo     = new InMemoryRepository<Payment>();

            // ── 2. Inițializare servicii cu injecție de dependențe (DIP) 
            INotificationSender notifier = new EmailNotificationSender();

            IAppointmentService appointmentService = new AppointmentService(
                appointmentRepo, doctorRepo, patientRepo, notifier);

            ITreatmentService treatmentService = new TreatmentService(
                treatmentRepo, appointmentRepo);

            IPaymentService paymentService = new PaymentService(
                paymentRepo, appointmentRepo);

            // ─────────────────────────────────────────────────────────
            // DEMO: creăm date de test
            // ─────────────────────────────────────────────────────────

            // ── Medici ─────────────────────────────────────────────────
            var doctor1 = new Doctor(
                "Alexandru", "Munteanu",
                "+373 69 111 222", "a.munteanu@dental.md",
                DoctorSpecialization.GeneralDentist, 300m);

            doctor1.SetWorkingHours(DayOfWeek.Monday,    new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
            doctor1.SetWorkingHours(DayOfWeek.Wednesday, new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));
            doctor1.SetWorkingHours(DayOfWeek.Friday,    new TimeSpan(8, 0, 0), new TimeSpan(14, 0, 0));

            var doctor2 = new Doctor(
                "Elena", "Codreanu",
                "+373 69 333 444", "e.codreanu@dental.md",
                DoctorSpecialization.Orthodontist, 500m);

            doctor2.SetWorkingHours(DayOfWeek.Tuesday,   new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
            doctor2.SetWorkingHours(DayOfWeek.Thursday,  new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));

            doctorRepo.Add(doctor1);
            doctorRepo.Add(doctor2);

            // ── Pacienți ───────────────────────────────────────────────
            var patient1 = new Patient(
                "Ion", "Popescu",
                "+373 79 555 666", "ion.popescu@email.md",
                new DateTime(1990, 5, 15), "Str. Ștefan cel Mare, 10, Chișinău");

            patient1.AddMedicalNote("Alergie la penicilină");
            patient1.AddMedicalNote("Diabet tip 2");

            var patient2 = new Patient(
                "Maria", "Ionescu",
                "+373 79 777 888", "maria.ionescu@email.md",
                new DateTime(1985, 8, 22), "Str. Florilor, 5, Chișinău");

            patientRepo.Add(patient1);
            patientRepo.Add(patient2);

            // ── Afișare entități create ────────────────────────────────
            Console.WriteLine("── MEDICI ÎNREGISTRAȚI ──────────────────────────────");
            foreach (var d in doctorRepo.GetAll()) Console.WriteLine(d);

            Console.WriteLine("\n── PACIENȚI ÎNREGISTRAȚI ────────────────────────────");
            foreach (var p in patientRepo.GetAll()) Console.WriteLine(p);
            Console.WriteLine($"   Note medicale {patient1.FullName}: {string.Join(", ", patient1.MedicalNotes)}");

            // ── Programări ─────────────────────────────────────────────
            Console.WriteLine("\n── CREARE PROGRAMĂRI ────────────────────────────────");

            // Găsim o dată validă: luni viitoare la 10:00
            DateTime nextMonday = GetNextWeekday(DayOfWeek.Monday).AddHours(10);
            DateTime nextTuesday = GetNextWeekday(DayOfWeek.Tuesday).AddHours(9).AddMinutes(30);

            appointmentService.BookAppointment(
                patient1.Id, doctor1.Id, nextMonday, TreatmentType.Consultation);

            appointmentService.BookAppointment(
                patient2.Id, doctor2.Id, nextTuesday, TreatmentType.Orthodontics);

            // ── Confirmare și finalizare tratament ─────────────────────
            Console.WriteLine("\n── CONFIRMARE ȘI FINALIZARE TRATAMENT ───────────────");
            var appointments = appointmentRepo.GetAll().ToList();

            appointments[0].Confirm();
            Console.WriteLine($"[OK] {appointments[0]} → Confirmat");

            treatmentService.RecordTreatment(
                appointments[0].Id,
                "Carie pe molarul superior dreapta",
                "Obturație compozit",
                450m);

            // ── Plăți ──────────────────────────────────────────────────
            Console.WriteLine("\n── PROCESARE PLĂȚI ──────────────────────────────────");
            paymentService.ProcessPayment(appointments[0].Id, 450m, PaymentMethod.Card);

            // ── Istoricul tratamentelor ────────────────────────────────
            Console.WriteLine("\n── ISTORICUL TRATAMENTELOR ──────────────────────────");
            foreach (TreatmentRecord rec in treatmentService.GetTreatmentHistory(patient1.Id))
                Console.WriteLine(rec.GetTreatmentSummary());

            // ── Raport financiar ───────────────────────────────────────
            Console.WriteLine("\n── RAPORT FINANCIAR ─────────────────────────────────");
            var revenue = paymentService.GetTotalRevenue(DateTime.Today, DateTime.Today.AddDays(1));
            Console.WriteLine($"Venituri astăzi: {revenue:C}");

            // ── Anulare programare ─────────────────────────────────────
            Console.WriteLine("\n── ANULARE PROGRAMARE ───────────────────────────────");
            appointmentService.CancelAppointment(appointments[1].Id, "Pacient indisponibil");

            Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║               DEMO FINALIZAT CU SUCCES               ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        }

        // Helper: găsim următoarea zi a săptămânii din viitor
        private static DateTime GetNextWeekday(DayOfWeek day)
        {
            var date = DateTime.Today.AddDays(1);
            while (date.DayOfWeek != day) date = date.AddDays(1);
            return date;
        }
    }
}
