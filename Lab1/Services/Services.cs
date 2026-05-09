using DentalClinic.Entities;
using DentalClinic.Enums;
using DentalClinic.Interfaces;

namespace DentalClinic.Services
{
    // ══════════════════════════════════════════════════════════════════
    // AppointmentService
    // SRP  – gestionează exclusiv logica de business a programărilor.
    // DIP  – depinde de IRepository<T>, nu de implementări concrete.
    // ══════════════════════════════════════════════════════════════════
    public class AppointmentService : IAppointmentService
    {
        private readonly IRepository<Appointment> _appointmentRepo;
        private readonly IRepository<Doctor>      _doctorRepo;
        private readonly IRepository<Patient>     _patientRepo;
        private readonly INotificationSender      _notifier;

        // DIP: dependențele sunt injectate, nu instanțiate intern
        public AppointmentService(
            IRepository<Appointment> appointmentRepo,
            IRepository<Doctor>      doctorRepo,
            IRepository<Patient>     patientRepo,
            INotificationSender      notifier)
        {
            _appointmentRepo = appointmentRepo;
            _doctorRepo      = doctorRepo;
            _patientRepo     = patientRepo;
            _notifier        = notifier;
        }

        public void BookAppointment(
            int           patientId,
            int           doctorId,
            DateTime      dateTime,
            TreatmentType treatmentType)
        {
            var patient = _patientRepo.GetById(patientId)
                ?? throw new KeyNotFoundException($"Pacientul {patientId} nu există.");

            var doctor = _doctorRepo.GetById(doctorId)
                ?? throw new KeyNotFoundException($"Medicul {doctorId} nu există.");

            if (!doctor.IsAvailable(dateTime))
                throw new InvalidOperationException(
                    $"Dr. {doctor.FullName} nu este disponibil la {dateTime:dd.MM.yyyy HH:mm}.");

            var appointment = new Appointment(patientId, doctorId, dateTime, treatmentType);
            _appointmentRepo.Add(appointment);

            // Notificare (OCP: orice canal de notificare poate fi injectat)
            _notifier.Send(
                patient.Email,
                "Confirmare programare",
                $"Stimate(ă) {patient.FullName},\n" +
                $"Programarea dvs. la Dr. {doctor.FullName} a fost înregistrată " +
                $"pentru {dateTime:dd.MM.yyyy} la ora {dateTime:HH:mm}.\n" +
                $"Tratament: {treatmentType}.");

            Console.WriteLine($"[OK] Programare creată: {appointment}");
        }

        public void CancelAppointment(int appointmentId, string reason)
        {
            var appointment = _appointmentRepo.GetById(appointmentId)
                ?? throw new KeyNotFoundException($"Programarea {appointmentId} nu există.");

            appointment.Cancel(reason);
            _appointmentRepo.Update(appointment);
            Console.WriteLine($"[OK] Programarea #{appointmentId} a fost anulată. Motiv: {reason}");
        }

        public IEnumerable<object> GetAppointmentsByPatient(int patientId) =>
            _appointmentRepo.GetAll()
                .Where(a => a.PatientId == patientId)
                .Cast<object>();

        public IEnumerable<object> GetAppointmentsByDoctor(int doctorId, DateTime date) =>
            _appointmentRepo.GetAll()
                .Where(a => a.DoctorId == doctorId && a.DateTime.Date == date.Date)
                .Cast<object>();
    }

    // ══════════════════════════════════════════════════════════════════
    // TreatmentService
    // SRP  – gestionează exclusiv logica înregistrărilor de tratament.
    // ══════════════════════════════════════════════════════════════════
    public class TreatmentService : ITreatmentService
    {
        private readonly IRepository<TreatmentRecord> _treatmentRepo;
        private readonly IRepository<Appointment>     _appointmentRepo;

        public TreatmentService(
            IRepository<TreatmentRecord> treatmentRepo,
            IRepository<Appointment>     appointmentRepo)
        {
            _treatmentRepo   = treatmentRepo;
            _appointmentRepo = appointmentRepo;
        }

        public void RecordTreatment(
            int     appointmentId,
            string  diagnosis,
            string  procedure,
            decimal cost)
        {
            var appointment = _appointmentRepo.GetById(appointmentId)
                ?? throw new KeyNotFoundException($"Programarea {appointmentId} nu există.");

            // Marchează programarea ca finalizată
            appointment.Complete();
            _appointmentRepo.Update(appointment);

            var record = new TreatmentRecord(
                appointmentId,
                appointment.PatientId,
                appointment.DoctorId,
                appointment.TreatmentType,
                diagnosis,
                procedure,
                cost);

            _treatmentRepo.Add(record);
            Console.WriteLine($"[OK] Tratament înregistrat: {record}");
        }

        public IEnumerable<object> GetTreatmentHistory(int patientId) =>
            _treatmentRepo.GetAll()
                .Where(t => t.PatientId == patientId)
                .OrderByDescending(t => t.Date)
                .Cast<object>();
    }

    // ══════════════════════════════════════════════════════════════════
    // PaymentService
    // SRP  – gestionează exclusiv logica financiară.
    // ══════════════════════════════════════════════════════════════════
    public class PaymentService : IPaymentService
    {
        private readonly IRepository<Payment>     _paymentRepo;
        private readonly IRepository<Appointment> _appointmentRepo;

        public PaymentService(
            IRepository<Payment>     paymentRepo,
            IRepository<Appointment> appointmentRepo)
        {
            _paymentRepo     = paymentRepo;
            _appointmentRepo = appointmentRepo;
        }

        public void ProcessPayment(int appointmentId, decimal amount, PaymentMethod method)
        {
            // Găsim sau creăm payment-ul pentru această programare
            var payment = _paymentRepo.GetAll()
                .FirstOrDefault(p => p.AppointmentId == appointmentId);

            if (payment == null)
            {
                var appointment = _appointmentRepo.GetById(appointmentId)
                    ?? throw new KeyNotFoundException($"Programarea {appointmentId} nu există.");

                payment = new Payment(appointmentId, appointment.PatientId, amount);
                _paymentRepo.Add(payment);
            }

            payment.ProcessPayment(amount, method);
            _paymentRepo.Update(payment);
            Console.WriteLine($"[OK] Plată procesată: {payment}");
        }

        public decimal GetTotalRevenue(DateTime from, DateTime to) =>
            _paymentRepo.GetAll()
                .Where(p => p.CreatedAt >= from && p.CreatedAt <= to)
                .Sum(p => p.AmountPaid);

        public IEnumerable<object> GetOverduePayments() =>
            _paymentRepo.GetAll()
                .Where(p => p.Status == PaymentStatus.Pending
                         || p.Status == PaymentStatus.PartiallyPaid)
                .Cast<object>();
    }

    // ══════════════════════════════════════════════════════════════════
    // EmailNotificationSender  (OCP: putem adăuga SMS etc. fără modificare)
    // ══════════════════════════════════════════════════════════════════
    public class EmailNotificationSender : INotificationSender
    {
        public void Send(string recipient, string subject, string body)
        {
            // Simulare trimitere email (într-un proiect real → SMTP / SendGrid)
            Console.WriteLine($"\n[EMAIL → {recipient}]");
            Console.WriteLine($"  Subiect : {subject}");
            Console.WriteLine($"  Mesaj   : {body}\n");
        }
    }

    /// <summary>
    /// Notificare prin consolă – utilă pentru teste (OCP demonstrat).
    /// </summary>
    public class ConsoleNotificationSender : INotificationSender
    {
        public void Send(string recipient, string subject, string body) =>
            Console.WriteLine($"[NOTIFICARE pentru {recipient}]: {subject}");
    }
}
