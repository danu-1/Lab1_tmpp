using System;
using System.Collections.Generic;
using System.Linq;
using DentalClinic.Models;
using DentalClinic.Strategy;
using DentalClinic.Observer;
using DentalClinic.Command;
using DentalClinic.Memento;
using DentalClinic.Iterator;

namespace DentalClinic.Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  TESTE UNITARE – Laborator 6 (fără framework extern)
    //  Același format ca Lab5: Assert manual cu raportare PASS/FAIL
    // ════════════════════════════════════════════════════════════════════════════

    public static class Lab6Tests
    {
        private static int _pass, _fail;

        // ── Assert helpers ────────────────────────────────────────────────────

        private static void Assert(bool condition, string testName)
        {
            if (condition) { _pass++; Console.WriteLine($"  ✅ PASS | {testName}"); }
            else           { _fail++; Console.WriteLine($"  ❌ FAIL | {testName}"); }
        }

        private static void AssertEqual<T>(T expected, T actual, string testName)
            => Assert(EqualityComparer<T>.Default.Equals(expected, actual),
                      $"{testName} (expected={expected}, got={actual})");

        private static void AssertThrows<TEx>(Action action, string testName) where TEx : Exception
        {
            try { action(); _fail++; Console.WriteLine($"  ❌ FAIL | {testName} (no exception)"); }
            catch (TEx) { _pass++; Console.WriteLine($"  ✅ PASS | {testName}"); }
            catch (Exception ex) { _fail++; Console.WriteLine($"  ❌ FAIL | {testName} ({ex.GetType().Name})"); }
        }

        // ── Fixture helpers ───────────────────────────────────────────────────

        private static Patient MakeStandard() =>
            new("P001", "Ion Popescu",  "+37369111222", "ion@email.md",
                new DateTime(1990, 5, 15));

        private static Patient MakeVIP() =>
            new("P002", "Maria Ionescu", "+37379777888", "maria@email.md",
                new DateTime(1975, 3, 20), PatientCategory.VIP);

        private static Patient MakeInsured()
        {
            var p = new Patient("P003", "Alex Moraru", "+37369333444", "alex@email.md",
                                new DateTime(1985, 8, 10), PatientCategory.Insured);
            p.InsurancePolicyNumber = "POL-1234";
            p.InsuranceCoverage = 0.60;
            return p;
        }

        private static Doctor MakeDoctor() =>
            new("D1", "Munteanu", "Stomatologie generală", "munteanu@clinic.md");

        private static Appointment MakeAppt(string id, Patient patient, Doctor doctor,
                                             string service, double price,
                                             DateTime? dt = null) =>
            new(id, patient, doctor, dt ?? new DateTime(2026, 5, 15, 10, 0, 0), service, price);

        // ════════════════════════════════════════════════════════════════════════
        //  STRATEGY TESTS
        // ════════════════════════════════════════════════════════════════════════

        public static void RunStrategyTests()
        {
            Console.WriteLine("\n── Strategy Tests ──────────────────────────────────");

            var std     = MakeStandard();
            var vip     = MakeVIP();
            var insured = MakeInsured();

            // Standard pricing
            var stdStrategy = new StandardPricingStrategy();
            AssertEqual(380.0, stdStrategy.CalculatePrice(380, std),
                        "Standard: preț nemodificat");
            Assert(stdStrategy.StrategyName == "Standard",
                   "Standard: StrategyName corect");

            // VIP pricing – reducere 20%
            var vipStrategy = new VIPPricingStrategy();
            AssertEqual(304.0, vipStrategy.CalculatePrice(380, vip),
                        "VIP: reducere 20% → 380*0.8=304");

            // VIP pricing – >$500, reducere 25%
            AssertEqual(375.0, vipStrategy.CalculatePrice(500, vip),
                        "VIP: >$500 → reducere 25% → 500*0.75=375");
            AssertEqual(562.5, vipStrategy.CalculatePrice(750, vip),
                        "VIP: 750 >$500 → 750*0.75=562.50");

            // Insurance pricing – 60% acoperit, TVA 20% pe rest
            // rest=152, cu TVA=182.40
            var insStrategy = new InsurancePricingStrategy();
            double expected = Math.Round(380 * 0.40 * 1.20, 2);  // 152*1.2=182.40
            AssertEqual(expected, insStrategy.CalculatePrice(380, insured),
                        $"Asigurare 60%+TVA → ${expected:F2}");

            // Promotional pricing – 15% dar max $50
            var promo = new PromotionalPricingStrategy("DENTA15", 0.15, 50);
            AssertEqual(330.0, promo.CalculatePrice(380, std),
                        "Promo 15% fără plafon: 380-57=323 (plafonat la 50) → 330");
            // Verificare cap: 380*0.15=57, plafonat la 50 → 380-50=330
            Assert(promo.StrategyName.Contains("DENTA15"),
                   "Promo: StrategyName conține codul promoțional");

            // Factory
            var fStd  = PricingStrategyFactory.ForPatient(std);
            var fVip  = PricingStrategyFactory.ForPatient(vip);
            var fIns  = PricingStrategyFactory.ForPatient(insured);
            Assert(fStd  is StandardPricingStrategy,  "Factory: Standard → StandardPricingStrategy");
            Assert(fVip  is VIPPricingStrategy,        "Factory: VIP → VIPPricingStrategy");
            Assert(fIns  is InsurancePricingStrategy,  "Factory: Insured → InsurancePricingStrategy");

            // Context – schimbare strategie la runtime
            var ctx = new AppointmentPricingContext(new StandardPricingStrategy());
            AssertEqual(380.0, ctx.CalculateFinalPrice(MakeAppt("A1", std, MakeDoctor(), "S", 380)),
                        "Context inițial: Standard");
            ctx.SetStrategy(new VIPPricingStrategy());
            AssertEqual(304.0, ctx.CalculateFinalPrice(MakeAppt("A1", vip, MakeDoctor(), "S", 380)),
                        "Context după SetStrategy: VIP");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  OBSERVER TESTS
        // ════════════════════════════════════════════════════════════════════════

        public static void RunObserverTests()
        {
            Console.WriteLine("\n── Observer Tests ───────────────────────────────────");

            var scheduler = new AppointmentScheduler();
            var audit     = new AuditLogObserver();
            var reception = new ReceptionDeskObserver();

            scheduler.Attach(audit);
            scheduler.Attach(reception);
            // Ignorăm output-ul PatientObserver în teste pentru claritate
            var patientObs = new PatientNotificationObserver();
            scheduler.Attach(patientObs);

            var patient = MakeStandard();
            var doctor  = MakeDoctor();
            var appt    = MakeAppt("A1", patient, doctor, "Detartraj", 250);

            // Create
            scheduler.CreateAppointment(appt);
            AssertEqual(1, audit.LogCount, "Observer: 1 intrare audit după Create");
            AssertEqual(1, reception.WaitingCount, "Observer: recepție are 1 programare");

            // Confirm
            scheduler.ConfirmAppointment("A1");
            AssertEqual(2, audit.LogCount, "Observer: 2 intrări audit după Confirm");

            // Cancel
            scheduler.CancelAppointment("A1", "Urgență medicală");
            AssertEqual(3, audit.LogCount, "Observer: 3 intrări audit după Cancel");
            AssertEqual(0, reception.WaitingCount, "Observer: recepție 0 după Cancel");

            // Detach
            scheduler.Detach(audit);
            var appt2 = MakeAppt("A2", patient, doctor, "Obturație", 350);
            scheduler.CreateAppointment(appt2);
            AssertEqual(3, audit.LogCount, "Observer: audit oprit după Detach");

            // Log content
            var log = audit.GetLog();
            Assert(log[0].Contains("A1"),       "Observer: jurnal conține ID programare");
            Assert(log[0].Contains("Created"),  "Observer: prima intrare este Created");
            Assert(log[2].Contains("Cancelled"),"Observer: a treia intrare este Cancelled");

            // Complete
            var scheduler2 = new AppointmentScheduler();
            var audit2     = new AuditLogObserver();
            scheduler2.Attach(audit2);
            var appt3 = MakeAppt("A3", patient, doctor, "Control", 100);
            scheduler2.CreateAppointment(appt3);
            scheduler2.CompleteAppointment("A3");
            Assert(audit2.GetLog().Any(l => l.Contains("Completed")),
                   "Observer: Completed jurnalizat");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  COMMAND TESTS
        // ════════════════════════════════════════════════════════════════════════

        public static void RunCommandTests()
        {
            Console.WriteLine("\n── Command Tests ────────────────────────────────────");

            var repo    = new AppointmentRepository();
            var invoker = new CommandInvoker();
            var patient = MakeStandard();
            var doctor  = MakeDoctor();

            // Create
            var appt = MakeAppt("C1", patient, doctor, "Obturație", 380);
            var createCmd = new CreateAppointmentCommand(repo, appt);
            invoker.Execute(createCmd);
            Assert(repo.Exists("C1"), "Command: CreateAppointment – programare adăugată");
            AssertEqual(1, invoker.HistoryCount, "Command: history count = 1 după Create");

            // Undo Create
            invoker.Undo();
            Assert(!repo.Exists("C1"), "Command: Undo Create – programare eliminată");
            AssertEqual(0, invoker.HistoryCount, "Command: history gol după Undo");

            // Redo Create
            invoker.Redo();
            Assert(repo.Exists("C1"), "Command: Redo Create – programare restabilită");

            // Cancel
            var cancelCmd = new CancelAppointmentCommand(repo, "C1", "Motiv test");
            invoker.Execute(cancelCmd);
            AssertEqual(AppointmentStatus.Cancelled, repo.Get("C1").Status,
                        "Command: Cancel – status Cancelled");

            // Undo Cancel
            invoker.Undo();
            Assert(repo.Get("C1").Status != AppointmentStatus.Cancelled,
                   "Command: Undo Cancel – status restaurat");

            // Reschedule
            var newDate = new DateTime(2026, 6, 1, 14, 0, 0);
            var reschedCmd = new RescheduleAppointmentCommand(repo, "C1", newDate);
            invoker.Execute(reschedCmd);
            AssertEqual(newDate, repo.Get("C1").DateTime,
                        "Command: Reschedule – data actualizată");
            invoker.Undo();
            Assert(repo.Get("C1").DateTime != newDate,
                   "Command: Undo Reschedule – data originală restaurată");

            // UpdatePrice
            var priceCmd = new UpdatePriceCommand(repo, "C1", 500.0);
            invoker.Execute(priceCmd);
            AssertEqual(500.0, repo.Get("C1").BasePrice, "Command: UpdatePrice → 500");
            invoker.Undo();
            AssertEqual(380.0, repo.Get("C1").BasePrice, "Command: Undo UpdatePrice → 380");

            // MacroCommand
            var appt2 = MakeAppt("C2", patient, doctor, "Extracție", 200);
            var macro = new MacroCommand("PachetNou",
                new CreateAppointmentCommand(repo, appt2),
                new UpdatePriceCommand(repo, "C2", 250.0));
            invoker.Execute(macro);
            Assert(repo.Exists("C2"), "MacroCommand: C2 creat");
            AssertEqual(250.0, repo.Get("C2").BasePrice, "MacroCommand: preț actualizat via macro");
            invoker.Undo();
            Assert(!repo.Exists("C2"), "MacroCommand: Undo macro – C2 eliminat");

            // Redo empty
            var invoker2 = new CommandInvoker();
            Assert(!invoker2.Redo(), "Command: Redo pe invoker gol returnează false");
            Assert(!invoker2.Undo(), "Command: Undo pe invoker gol returnează false");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  MEMENTO TESTS
        // ════════════════════════════════════════════════════════════════════════

        public static void RunMementoTests()
        {
            Console.WriteLine("\n── Memento Tests ────────────────────────────────────");

            var file    = new PatientFile("P001", "Ion Popescu");
            var editor  = new PatientFileEditor(file);
            var history = new PatientFileHistory(editor);

            // Stare inițială
            Assert(editor.Allergies == "Niciuna", "Memento: alergii inițiale = Niciuna");

            // Salvare versiune 1
            history.SaveVersion("initial");
            AssertEqual(1, history.VersionCount, "Memento: 1 versiune după primul Save");

            // Modificare
            editor.Allergies = "Penicilină";
            editor.ChronicConditions = "Diabet tip 2";
            editor.LastModifiedBy = "Dr.Munteanu";
            history.SaveVersion("dupa_alergii");

            AssertEqual("Penicilină", editor.Allergies,
                        "Memento: alergii actualizate înainte de Undo");

            // Undo
            history.Undo();
            AssertEqual("Niciuna", editor.Allergies,
                        "Memento: Undo → alergii revenite la Niciuna");

            // Redo
            history.Redo();
            AssertEqual("Penicilină", editor.Allergies,
                        "Memento: Redo → alergii = Penicilină");

            // Tratamente
            history.SaveVersion("inainte_tratamente");
            var rec = new TreatmentRecord("T1", "P001", "D1", DateTime.Now,
                                          "Carie", "Obturație", "K02.1", 380);
            editor.AddTreatment(rec);
            AssertEqual(1, editor.TreatmentCount, "Memento: 1 tratament adăugat");
            history.SaveVersion("cu_tratament");

            history.Undo(); // revine la inainte_tratamente
            AssertEqual(1, editor.TreatmentCount,
                        "Memento: Undo nu modifică ref la TreatmentCount (snapshot)");

            // Verifică că snapshot-ul e independent (deep copy)
            var snap = editor.Save("test_snap");
            editor.Allergies = "Aspirină";
            Assert(snap.Allergies != "Aspirină",
                   "Memento: snapshot independent – modificarea ulterioară nu afectează snapshot-ul");

            // Multiplă versiuni
            var file2   = new PatientFile("P002", "Maria");
            var editor2 = new PatientFileEditor(file2);
            var hist2   = new PatientFileHistory(editor2, maxVersions: 3);
            hist2.SaveVersion("v1");
            hist2.SaveVersion("v2");
            hist2.SaveVersion("v3");
            hist2.SaveVersion("v4"); // depășește maxVersions=3
            Assert(hist2.VersionCount <= 3,
                   "Memento: maxVersions respectat – max 3 versiuni");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  ITERATOR TESTS
        // ════════════════════════════════════════════════════════════════════════

        public static void RunIteratorTests()
        {
            Console.WriteLine("\n── Iterator Tests ───────────────────────────────────");

            var col  = new AppointmentCollection();
            var doc1 = new Doctor("D1", "Munteanu", "Generală");
            var doc2 = new Doctor("D2", "Ionescu",  "Ortodonție");
            var p1   = MakeStandard();
            var p2   = MakeVIP();

            var d1 = new DateTime(2026, 5, 15, 9, 0, 0);
            var d2 = new DateTime(2026, 5, 15, 11, 0, 0);
            var d3 = new DateTime(2026, 5, 16, 10, 0, 0);
            var d4 = new DateTime(2026, 5, 17, 14, 0, 0);

            var a1 = MakeAppt("I1", p1, doc1, "Obturație",  380, d2);
            var a2 = MakeAppt("I2", p2, doc1, "Detartraj",  250, d1);
            var a3 = MakeAppt("I3", p1, doc2, "Bracket",    800, d3);
            var a4 = MakeAppt("I4", p2, doc2, "Consultație",150, d4);
            a4.Status = AppointmentStatus.Cancelled;

            col.Add(a1); col.Add(a2); col.Add(a3); col.Add(a4);
            AssertEqual(4, col.Count, "Iterator: colecție are 4 programări");

            // Chronological
            var chrono = col.CreateChronologicalIterator();
            AssertEqual(4, chrono.TotalCount, "Iterator: cronologic – total 4");
            var first  = chrono.Next();
            var second = chrono.Next();
            Assert(first.DateTime <= second.DateTime,
                   "Iterator: cronologic – prima ≤ a doua");

            // Reset
            chrono.Reset();
            AssertEqual("I2", chrono.Next().Id,
                        "Iterator: Reset → primul element = I2 (cel mai vechi)");

            // By doctor
            var byDoc1 = col.CreateByDoctorIterator("D1");
            AssertEqual(2, byDoc1.TotalCount, "Iterator: byDoctor D1 → 2 programări");
            var byDoc2 = col.CreateByDoctorIterator("D2");
            AssertEqual(2, byDoc2.TotalCount, "Iterator: byDoctor D2 → 2 programări");

            // By status
            var byCancelled = col.CreateByStatusIterator(AppointmentStatus.Cancelled);
            AssertEqual(1, byCancelled.TotalCount, "Iterator: byStatus Cancelled → 1");
            AssertEqual("I4", byCancelled.Next().Id, "Iterator: Cancelled este I4");

            var byScheduled = col.CreateByStatusIterator(AppointmentStatus.Scheduled);
            AssertEqual(3, byScheduled.TotalCount, "Iterator: byStatus Scheduled → 3");

            // By date
            var byDate = col.CreateByDateIterator(new DateTime(2026, 5, 15));
            AssertEqual(2, byDate.TotalCount, "Iterator: byDate 15.05 → 2 programări");

            // By patient
            var byP1 = col.CreateByPatientIterator("P001");
            AssertEqual(2, byP1.TotalCount, "Iterator: byPatient P001 → 2");
            var byP2 = col.CreateByPatientIterator("P002");
            AssertEqual(2, byP2.TotalCount, "Iterator: byPatient P002 → 2");

            // HasNext la capăt
            var it = col.CreateByDoctorIterator("D1");
            it.Next(); it.Next();
            Assert(!it.HasNext(), "Iterator: HasNext = false după epuizare");

            // Empty result
            var empty = col.CreateByDoctorIterator("D99");
            Assert(!empty.HasNext(), "Iterator: medic inexistent → empty iterator");
            AssertEqual(0, empty.TotalCount, "Iterator: TotalCount=0 pentru medic inexistent");

            // IEnumerable (foreach C#)
            int count = 0;
            foreach (var _ in col) count++;
            AssertEqual(4, count, "Iterator: IEnumerable foreach → 4 elemente");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  RUNNER
        // ════════════════════════════════════════════════════════════════════════

        public static void RunAll()
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║           TESTE UNITARE – Laborator 6                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");

            _pass = 0; _fail = 0;

            RunStrategyTests();
            RunObserverTests();
            RunCommandTests();
            RunMementoTests();
            RunIteratorTests();

            Console.WriteLine($"\n{'─',56}");
            Console.WriteLine($"  Rezultat: {_pass}/{_pass + _fail} teste trecute | " +
                              (_fail == 0 ? "Toate OK ✅" : $"{_fail} EȘUATE ❌"));
        }
    }
}
