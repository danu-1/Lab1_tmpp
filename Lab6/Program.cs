using System;
using DentalClinic.Models;
using DentalClinic.Strategy;
using DentalClinic.Observer;
using DentalClinic.Command;
using DentalClinic.Memento;
using DentalClinic.Iterator;
using DentalClinic.Tests;

namespace DentalClinic
{
    class Program
    {
        // ── Shared fixtures ───────────────────────────────────────────────────

        static readonly Patient PatIon = new("P001", "Ion Popescu",
            "+37369111222", "ion.popescu@email.md", new DateTime(1990, 5, 15));

        static readonly Patient PatMaria = new("P002", "Maria Ionescu",
            "+37379777888", "maria.ionescu@email.md", new DateTime(1975, 3, 20),
            PatientCategory.VIP);

        static readonly Patient PatAlex = new Patient("P003", "Alexandru Moraru",
            "+37369333444", "alex@email.md", new DateTime(1985, 8, 10),
            PatientCategory.Insured)
        { InsurancePolicyNumber = "POL-9981", InsuranceCoverage = 0.60 };

        static readonly Doctor DocMunteanu = new("D1", "Munteanu",    "Stomatologie generală", "munteanu@clinic.md");
        static readonly Doctor DocIonescu  = new("D2", "Ionescu",     "Ortodonție",             "ionescu@clinic.md");
        static readonly Doctor DocPopa     = new("D3", "Popa",        "Pedodonție",             "popa@clinic.md");

        // ─────────────────────────────────────────────────────────────────────

        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            PrintHeader("SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ",
                        "Lab 6 – Strategy · Observer · Command · Memento · Iterator");

            DemoStrategy();
            DemoObserver();
            DemoCommand();
            DemoMemento();
            DemoIterator();

            Lab6Tests.RunAll();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DEMO 1 – STRATEGY
        // ════════════════════════════════════════════════════════════════════════

        static void DemoStrategy()
        {
            PrintSection("DEMO 1 – STRATEGY (Calcul Cost Tratament)");

            var apptIon   = new Appointment("A01", PatIon,   DocMunteanu,
                                new DateTime(2026, 5, 20, 10, 0, 0), "Obturație compozit 2 fețe", 380);
            var apptMaria = new Appointment("A02", PatMaria, DocMunteanu,
                                new DateTime(2026, 5, 20, 11, 0, 0), "Fatetă ceramică", 750);
            var apptAlex  = new Appointment("A03", PatAlex,  DocIonescu,
                                new DateTime(2026, 5, 21, 14, 0, 0), "Tratament canal (2 canale)", 620);

            // [1a] Standard pricing
            Console.WriteLine("\n[1a] Pacient standard – StandardPricingStrategy:");
            var ctx = new AppointmentPricingContext(new StandardPricingStrategy());
            ctx.PrintPricingBreakdown(apptIon);

            // [1b] VIP pricing
            Console.WriteLine("\n[1b] Pacient VIP – VIPPricingStrategy:");
            ctx.SetStrategy(new VIPPricingStrategy());
            apptMaria.Patient = PatMaria;
            ctx.PrintPricingBreakdown(apptMaria);

            // [1b2] VIP >$500 (extra 5%)
            Console.WriteLine("\n[1b'] VIP cu serviciu >$500 (reducere suplimentară 5%):");
            ctx.PrintPricingBreakdown(apptMaria);

            // [1c] Insurance pricing
            Console.WriteLine("\n[1c] Pacient asigurat – InsurancePricingStrategy:");
            ctx.SetStrategy(new InsurancePricingStrategy());
            ctx.PrintPricingBreakdown(apptAlex);

            // [1d] Promotional pricing
            Console.WriteLine("\n[1d] Promoție sezonieră – PromotionalPricingStrategy (15%, max $50):");
            ctx.SetStrategy(new PromotionalPricingStrategy("SPRING15", 0.15, 50));
            ctx.PrintPricingBreakdown(apptIon);

            // [1e] Factory auto-selection
            Console.WriteLine("\n[1e] Factory – selectare automată a strategiei:");
            foreach (var patient in new[] { PatIon, PatMaria, PatAlex })
            {
                var strategy = PricingStrategyFactory.ForPatient(patient);
                var appt     = new Appointment("Ax", patient, DocMunteanu,
                                   DateTime.Now, "Consultație", 150);
                double price = strategy.CalculatePrice(150, patient);
                Console.WriteLine($"  {patient.Name,-22} ({patient.Category,-10}) → {strategy.StrategyName,-12} → ${price:F2}");
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DEMO 2 – OBSERVER
        // ════════════════════════════════════════════════════════════════════════

        static void DemoObserver()
        {
            PrintSection("DEMO 2 – OBSERVER (Notificări Multi-Actor)");

            var scheduler = new AppointmentScheduler();
            var audit     = new AuditLogObserver();
            var reception = new ReceptionDeskObserver();
            var doctorCal = new DoctorCalendarObserver();
            var patient   = new PatientNotificationObserver();

            Console.WriteLine("\n[2a] Atașare observatori:");
            scheduler.Attach(patient);
            scheduler.Attach(doctorCal);
            scheduler.Attach(reception);
            scheduler.Attach(audit);

            Console.WriteLine("\n[2b] Creare programare → toți observatorii notificați:");
            var appt1 = new Appointment("OB1", PatIon, DocMunteanu,
                            new DateTime(2026, 5, 20, 10, 0, 0), "Obturație", 380);
            scheduler.CreateAppointment(appt1);

            Console.WriteLine("\n[2c] Confirmare programare:");
            scheduler.ConfirmAppointment("OB1");

            Console.WriteLine("\n[2d] A doua programare (VIP):");
            var appt2 = new Appointment("OB2", PatMaria, DocMunteanu,
                            new DateTime(2026, 5, 20, 11, 30, 0), "Fatetă ceramică", 750);
            scheduler.CreateAppointment(appt2);

            Console.WriteLine("\n[2e] Reprogramare:");
            scheduler.RescheduleAppointment("OB2", new DateTime(2026, 5, 22, 14, 0, 0));

            Console.WriteLine("\n[2f] Anulare cu motiv:");
            scheduler.CancelAppointment("OB1", "Urgență la locul de muncă");

            Console.WriteLine("\n[2g] Finalizare:");
            scheduler.CompleteAppointment("OB2");

            Console.WriteLine("\n[2h] Detașare DoctorCalendar – nu mai primește notificări:");
            scheduler.Detach(doctorCal);
            var appt3 = new Appointment("OB3", PatAlex, DocIonescu,
                            new DateTime(2026, 5, 25, 9, 0, 0), "Bracket", 800);
            scheduler.CreateAppointment(appt3);

            Console.WriteLine($"\n  ── Jurnal audit ({audit.LogCount} intrări) ──");
            foreach (var entry in audit.GetLog())
                Console.WriteLine($"  {entry}");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DEMO 3 – COMMAND
        // ════════════════════════════════════════════════════════════════════════

        static void DemoCommand()
        {
            PrintSection("DEMO 3 – COMMAND (Programări + Undo/Redo)");

            var repo    = new AppointmentRepository();
            var invoker = new CommandInvoker();

            Console.WriteLine("\n[3a] Creare programare:");
            var a1 = new Appointment("CMD1", PatIon, DocMunteanu,
                         new DateTime(2026, 6, 2, 10, 0, 0), "Obturație", 380);
            invoker.Execute(new CreateAppointmentCommand(repo, a1));

            Console.WriteLine("\n[3b] Creare a doua programare:");
            var a2 = new Appointment("CMD2", PatMaria, DocIonescu,
                         new DateTime(2026, 6, 3, 14, 0, 0), "Fatetă ceramică", 750);
            invoker.Execute(new CreateAppointmentCommand(repo, a2));

            Console.WriteLine("\n[3c] Actualizare preț CMD1:");
            invoker.Execute(new UpdatePriceCommand(repo, "CMD1", 420));
            Console.WriteLine($"  Preț curent CMD1: ${repo.Get("CMD1").BasePrice:F2}");

            Console.WriteLine("\n[3d] Undo actualizare preț:");
            invoker.Undo();
            Console.WriteLine($"  Preț după Undo: ${repo.Get("CMD1").BasePrice:F2}");

            Console.WriteLine("\n[3e] Reprogramare CMD2:");
            var newDate = new DateTime(2026, 6, 10, 15, 0, 0);
            invoker.Execute(new RescheduleAppointmentCommand(repo, "CMD2", newDate));
            Console.WriteLine($"  Dată nouă CMD2: {repo.Get("CMD2").DateTime:dd.MM.yyyy HH:mm}");

            Console.WriteLine("\n[3f] Undo reprogramare:");
            invoker.Undo();
            Console.WriteLine($"  Data CMD2 după Undo: {repo.Get("CMD2").DateTime:dd.MM.yyyy HH:mm}");

            Console.WriteLine("\n[3g] Redo reprogramare:");
            invoker.Redo();
            Console.WriteLine($"  Data CMD2 după Redo: {repo.Get("CMD2").DateTime:dd.MM.yyyy HH:mm}");

            Console.WriteLine("\n[3h] Anulare CMD1 cu motiv:");
            invoker.Execute(new CancelAppointmentCommand(repo, "CMD1", "Pacient indisponibil"));
            Console.WriteLine($"  Status CMD1: {repo.Get("CMD1").Status}");

            Console.WriteLine("\n[3i] Undo anulare – restaurare:");
            invoker.Undo();
            Console.WriteLine($"  Status CMD1 după Undo: {repo.Get("CMD1").Status}");

            Console.WriteLine("\n[3j] Macro – creare + prețuire nouă pacient:");
            var a3 = new Appointment("CMD3", PatAlex, DocPopa,
                         new DateTime(2026, 6, 5, 9, 0, 0), "Control periimplantar", 200);
            invoker.Execute(new MacroCommand("InregistrarePacientNou",
                new CreateAppointmentCommand(repo, a3),
                new UpdatePriceCommand(repo, "CMD3", 250)));
            Console.WriteLine($"  CMD3 există: {repo.Exists("CMD3")}, Preț: ${repo.Get("CMD3").BasePrice:F2}");

            Console.WriteLine("\n[3k] Undo macro:");
            invoker.Undo();
            Console.WriteLine($"  CMD3 după undo macro: {(repo.Exists("CMD3") ? "există" : "eliminat")}");

            Console.WriteLine();
            invoker.PrintHistory();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DEMO 4 – MEMENTO
        // ════════════════════════════════════════════════════════════════════════

        static void DemoMemento()
        {
            PrintSection("DEMO 4 – MEMENTO (Versioning Dosar Pacient)");

            var file    = new PatientFile("P001", "Ion Popescu");
            var editor  = new PatientFileEditor(file);
            var history = new PatientFileHistory(editor);

            Console.WriteLine("\n[4a] Stare inițială dosar:");
            Console.WriteLine("  " + editor);
            history.SaveVersion("initial – dosar gol");

            Console.WriteLine("\n[4b] Adăugare alergii și afecțiuni cronice:");
            editor.LastModifiedBy   = "Dr. Munteanu";
            editor.Allergies        = "Penicilină, Aspirină";
            editor.ChronicConditions = "Diabet tip 2";
            Console.WriteLine($"  Alergii: {editor.Allergies}");
            Console.WriteLine($"  Afecțiuni: {editor.ChronicConditions}");
            history.SaveVersion("dupa_anamneza");

            Console.WriteLine("\n[4c] Adăugare tratament:");
            editor.AddTreatment(new TreatmentRecord("T1", "P001", "D1",
                new DateTime(2026, 5, 10), "Carie medie M46", "Obturație compozit", "K02.1", 380));
            history.SaveVersion("cu_tratament_1");

            Console.WriteLine("\n[4d] Modificare greșită (eroare de editare):");
            editor.Allergies = "EROARE – câmp corupt";
            editor.ChronicConditions = "EROARE";
            Console.WriteLine($"  Alergii corupte: {editor.Allergies}");

            Console.WriteLine("\n[4e] Undo – revenire la versiunea anterioară:");
            history.Undo();
            Console.WriteLine($"  Alergii restaurate: {editor.Allergies}");
            Console.WriteLine($"  Afecțiuni restaurate: {editor.ChronicConditions}");
            Console.WriteLine($"  Tratamente: {editor.TreatmentCount}");

            Console.WriteLine("\n[4f] Undo → revenire la dupa_anamneza:");
            history.Undo();
            Console.WriteLine($"  Tratamente după al doilea Undo: {editor.TreatmentCount}");
            Console.WriteLine($"  Alergii: {editor.Allergies}");

            Console.WriteLine("\n[4g] Redo – re-aplicare versiune cu tratament:");
            history.Redo();
            Console.WriteLine($"  Tratamente după Redo: {editor.TreatmentCount}");

            Console.WriteLine("\n[4h] Al doilea tratament + salvare:");
            editor.AddTreatment(new TreatmentRecord("T2", "P001", "D2",
                new DateTime(2026, 5, 17), "Tartru sever", "Detartraj + airflow", "K03.6", 250));
            history.SaveVersion("cu_doua_tratamente");

            Console.WriteLine("\n[4i] Undo la 'initial':");
            history.Undo();
            history.Undo();
            history.Undo();
            Console.WriteLine($"\n  Stare finală: {editor}");

            Console.WriteLine();
            history.PrintVersionList();
        }

        // ════════════════════════════════════════════════════════════════════════
        //  DEMO 5 – ITERATOR
        // ════════════════════════════════════════════════════════════════════════

        static void DemoIterator()
        {
            PrintSection("DEMO 5 – ITERATOR (Parcurgere Colecție Programări)");

            var col = new AppointmentCollection();

            // Populăm cu date de test
            var appointments = new[]
            {
                new Appointment("IT1", PatIon,   DocMunteanu, new DateTime(2026,5,15, 9,00,0), "Obturație",           380) { Status = AppointmentStatus.Completed },
                new Appointment("IT2", PatMaria, DocMunteanu, new DateTime(2026,5,15,10,30,0), "Fatetă ceramică",     750) { Status = AppointmentStatus.Confirmed  },
                new Appointment("IT3", PatAlex,  DocIonescu,  new DateTime(2026,5,15,14,00,0), "Bracket ceramic",     800) { Status = AppointmentStatus.Scheduled  },
                new Appointment("IT4", PatIon,   DocIonescu,  new DateTime(2026,5,16, 9,00,0), "Control bracket",     150) { Status = AppointmentStatus.Scheduled  },
                new Appointment("IT5", PatMaria, DocPopa,     new DateTime(2026,5,16,11,00,0), "Extracție M6",        420) { Status = AppointmentStatus.Cancelled   },
                new Appointment("IT6", PatAlex,  DocMunteanu, new DateTime(2026,5,17,15,00,0), "Detartraj + airflow", 250) { Status = AppointmentStatus.Scheduled  },
                new Appointment("IT7", PatIon,   DocMunteanu, new DateTime(2026,5,18,10,00,0), "Consultație urgență", 100) { Status = AppointmentStatus.Scheduled  },
            };
            foreach (var a in appointments) col.Add(a);

            Console.WriteLine($"\n  Colecție totală: {col.Count} programări\n");

            // [5a] Cronologic
            Console.WriteLine("[5a] Iterator cronologic:");
            AppointmentCollection.PrintAll(col.CreateChronologicalIterator(), "Toate, ordine cronologică");

            // [5b] By doctor
            Console.WriteLine("\n[5b] Iterator pe medic:");
            AppointmentCollection.PrintAll(col.CreateByDoctorIterator("D1"), "Dr. Munteanu");
            AppointmentCollection.PrintAll(col.CreateByDoctorIterator("D2"), "Dr. Ionescu");

            // [5c] By status
            Console.WriteLine("\n[5c] Iterator pe status:");
            AppointmentCollection.PrintAll(col.CreateByStatusIterator(AppointmentStatus.Scheduled), "Programate");
            AppointmentCollection.PrintAll(col.CreateByStatusIterator(AppointmentStatus.Cancelled), "Anulate");
            AppointmentCollection.PrintAll(col.CreateByStatusIterator(AppointmentStatus.Completed), "Finalizate");

            // [5d] By date
            Console.WriteLine("\n[5d] Iterator pe dată:");
            AppointmentCollection.PrintAll(col.CreateByDateIterator(new DateTime(2026, 5, 15)), "15 mai 2026");
            AppointmentCollection.PrintAll(col.CreateByDateIterator(new DateTime(2026, 5, 16)), "16 mai 2026");

            // [5e] By patient
            Console.WriteLine("\n[5e] Istoricul pacientului (cel mai recent primul):");
            AppointmentCollection.PrintAll(col.CreateByPatientIterator("P001"), "Ion Popescu");

            // [5f] Reset demonstrație
            Console.WriteLine("\n[5f] Demonstrație Reset – parcurgere de 2 ori același iterator:");
            var it = col.CreateByStatusIterator(AppointmentStatus.Scheduled);
            Console.WriteLine("  Prima parcurgere:");
            int pass1 = 0; while (it.HasNext()) { it.Next(); pass1++; }
            it.Reset();
            Console.WriteLine("  A doua parcurgere (după Reset):");
            int pass2 = 0; while (it.HasNext()) { it.Next(); pass2++; }
            Console.WriteLine($"  Pass1={pass1}, Pass2={pass2}, egal: {pass1 == pass2}");

            // [5g] foreach C# (IEnumerable)
            Console.WriteLine("\n[5g] foreach C# nativ (IEnumerable<Appointment>):");
            double totalRevenue = 0;
            foreach (var a in col)
                if (a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Confirmed)
                    totalRevenue += a.BasePrice;
            Console.WriteLine($"  Venituri confirmate + finalizate: ${totalRevenue:F2}");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static void PrintHeader(string title, string subtitle)
        {
            Console.WriteLine("╔══════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  {title,-52}║");
            Console.WriteLine($"║  {subtitle,-52}║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
        }

        static void PrintSection(string title)
        {
            Console.WriteLine($"\n{'═',54}");
            Console.WriteLine($"  {title}");
            Console.WriteLine(new string('═', 54));
        }
    }
}
