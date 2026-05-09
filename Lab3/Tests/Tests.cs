// ═══════════════════════════════════════════════════════════════════════
//  TESTE UNITARE – Builder, Prototype, Singleton
// ═══════════════════════════════════════════════════════════════════════

using DentalClinic.Lab3.Builder;
using DentalClinic.Lab3.Prototype;
using DentalClinic.Lab3.Singleton;

namespace DentalClinic.Lab3.Tests
{
    public static class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;

        private static void Assert(string name, bool ok, string? msg = null)
        {
            if (ok) { Console.WriteLine($"  ✅ PASS | {name}"); _passed++; }
            else    { Console.WriteLine($"  ❌ FAIL | {name}" + (msg != null ? $" → {msg}" : "")); _failed++; }
        }

        public static void RunAll()
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║           TESTE UNITARE – Laborator 3                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");

            RunBuilderTests();
            RunPrototypeTests();
            RunSingletonTests();
            RunSingletonThreadSafetyTest();
            PrintSummary();
        }

        // ══════════════════════════════════════════════════════════════
        // BUILDER TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunBuilderTests()
        {
            Console.WriteLine("\n── Builder Tests ────────────────────────────────────");

            var builder = new TreatmentPlanBuilder();

            // Test 1: Plan construit corect cu Fluent API
            var plan = builder
                .ForPatient("Ion Popescu")
                .ByDoctor("Dr. Munteanu")
                .WithDiagnosis("Carie")
                .AddProcedure("Obturație", "Molar 1", 30, 300m)
                .Build();

            Assert("Builder produce TreatmentPlan non-null", plan != null);
            Assert("PatientName setat corect", plan!.PatientName == "Ion Popescu");
            Assert("DoctorName setat corect",  plan.DoctorName   == "Dr. Munteanu");
            Assert("Diagnosis setat corect",   plan.Diagnosis    == "Carie");
            Assert("O procedură adăugată",     plan.Procedures.Count == 1);
            Assert("PlanId generat (TP-XXXX)", plan.PlanId.StartsWith("TP-"));

            // Test 2: TotalCost calculat corect
            Assert("TotalCost = suma procedurilor",
                plan.TotalCost == 300m);

            // Test 3: TotalCost cu Discount
            var planDisc = builder
                .ForPatient("Maria Ionescu")
                .ByDoctor("Dr. Codreanu")
                .AddProcedure("Detartraj", "Complet", 30, 200m)
                .AddProcedure("Fluorurare","Complet",  15,  60m)
                .WithDiscount(0.10m)
                .Build();

            decimal expected = Math.Round((200m + 60m) * 0.90m, 2);
            Assert($"TotalCost cu discount 10% = {expected}",
                planDisc.TotalCost == expected);

            // Test 4: TotalDurationMins
            Assert("TotalDurationMins calculat",
                planDisc.TotalDurationMins == 45);

            // Test 5: Proceduri multiple
            var planMulti = builder
                .ForPatient("Test")
                .ByDoctor("Dr. Test")
                .AddProcedure("P1", "Z1", 10, 100m)
                .AddProcedure("P2", "Z2", 20, 200m)
                .AddProcedure("P3", "Z3", 30, 300m)
                .Build();
            Assert("3 proceduri adăugate", planMulti.Procedures.Count == 3);

            // Test 6: Medicamente adăugate
            var planMed = builder
                .ForPatient("Test2")
                .ByDoctor("Dr. Test")
                .AddProcedure("P1","Z1", 10, 100m)
                .AddMedication("Amoxicilină","500mg",7)
                .AddMedication("Ibuprofen",  "400mg",3)
                .Build();
            Assert("2 medicamente adăugate", planMed.Medications.Count == 2);

            // Test 7: Anestezie setată
            var planAn = builder
                .ForPatient("Test3")
                .ByDoctor("Dr. Test")
                .AddProcedure("Extracție","M3",20,250m)
                .RequiresAnesthesia()
                .Build();
            Assert("RequiresAnesthesia = true", planAn.RequiresAnesthesia);

            // Test 8: Asigurare setată
            var planIns = builder
                .ForPatient("Test4")
                .ByDoctor("Dr. Test")
                .AddProcedure("Consultație","Complet",15,200m)
                .WithInsurance("Donaris VIG")
                .Build();
            Assert("InsuranceCovered = true",        planIns.InsuranceCovered);
            Assert("InsuranceProvider = Donaris VIG", planIns.InsuranceProvider == "Donaris VIG");

            // Test 9: Builder se resetează după Build
            try
            {
                builder.Build(); // fără ForPatient → excepție așteptată
                Assert("Build fără pacient aruncă excepție", false);
            }
            catch (InvalidOperationException)
            {
                Assert("Build fără pacient aruncă excepție", true);
            }

            // Test 10: Discount invalid aruncă excepție
            try
            {
                builder.WithDiscount(1.5m);
                Assert("Discount > 1 aruncă excepție", false);
            }
            catch (ArgumentException)
            {
                Assert("Discount > 1 aruncă excepție", true);
            }

            // Test 11: Director – Emergency plan
            var director = new TreatmentPlanDirector(builder);
            var emergency = director.BuildEmergencyExtractionPlan("Pacient E", "Dr. X");
            Assert("Director: emergency plan – RequiresAnesthesia",
                emergency.RequiresAnesthesia);
            Assert("Director: emergency plan – Urgency = URGENTĂ",
                emergency.Urgency == "URGENTĂ");

            // Test 12: Director – Hygiene plan fără anestezie
            var hygiene = director.BuildAnnualHygienePlan("Pacient H", "Dr. X");
            Assert("Director: hygiene plan – nu necesită anestezie",
                !hygiene.RequiresAnesthesia);

            // Test 13: Director – Orthodontic cu reducere student
            var ortho = director.BuildOrthodonticPlan("Student S", "Dr. X", true);
            Assert("Director: orthodontic plan cu reducere student – Discount = 15%",
                ortho.Discount == 0.15m);

            // Test 14: Programări adăugate
            var dt = DateTime.Now.AddDays(5);
            var planAppt = builder
                .ForPatient("Test5").ByDoctor("Dr.Y")
                .AddProcedure("P","Z",10,100m)
                .ScheduleAppointment(dt)
                .ScheduleAppointment(dt.AddMonths(1))
                .Build();
            Assert("2 programări adăugate", planAppt.Appointments.Count == 2);

            // Test 15: AfterCare notes
            var planAc = builder
                .ForPatient("Test6").ByDoctor("Dr.Y")
                .AddProcedure("P","Z",10,100m)
                .AddAfterCareNote("Nota 1")
                .AddAfterCareNote("Nota 2")
                .Build();
            Assert("2 AfterCareNotes adăugate", planAc.AfterCareNotes.Count == 2);
        }

        // ══════════════════════════════════════════════════════════════
        // PROTOTYPE TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunPrototypeTests()
        {
            Console.WriteLine("\n── Prototype Tests ──────────────────────────────────");

            // Setup: creăm un prototip original
            var original = new PatientRecord
            {
                PatientName  = "Template Diabetic",
                Age          = 0,
                BloodType    = "Necunoscut",
                HasDiabetes  = true,
                DentistNotes = "Verificare glicemie pre-intervenție.",
                Allergies    = new List<Allergy>
                {
                    new("Penicilină", "Severă"),
                    new("Latex",      "Moderată")
                },
                Medications = new List<string> { "Metformin", "Insulină" }
            };

            // Test 1: DeepClone – ID diferit
            var deepClone = original.DeepClone();
            Assert("DeepClone produce ID diferit",
                deepClone.RecordId != original.RecordId);

            // Test 2: DeepClone – câmpuri copiete corect
            Assert("DeepClone: PatientName copiat",  deepClone.PatientName == original.PatientName);
            Assert("DeepClone: HasDiabetes copiat",  deepClone.HasDiabetes == original.HasDiabetes);
            Assert("DeepClone: Alergii copiate",     deepClone.Allergies.Count == 2);

            // Test 3: Deep Clone – independență liste (modificare clonă nu afectează originalul)
            deepClone.Allergies.Add(new Allergy("Aspirină", "Ușoară"));
            Assert("DeepClone: adăugare în clonă NU afectează originalul",
                original.Allergies.Count == 2);

            deepClone.PatientName = "Ion Popescu";
            Assert("DeepClone: modificare câmp primitiv în clonă nu afectează originalul",
                original.PatientName == "Template Diabetic");

            // Test 4: ShallowClone – ID diferit
            var shallow = original.ShallowClone();
            Assert("ShallowClone produce ID diferit",
                shallow.RecordId != original.RecordId);

            // Test 5: ShallowClone – listele SUNT aceleași referințe
            Assert("ShallowClone: liste sunt aceeași referință",
                ReferenceEquals(shallow.Allergies, original.Allergies));

            // Test 6: TreatmentTemplate – DeepClone
            var tmpl = new TreatmentTemplate
            {
                TemplateName = "Protocol extracție",
                Category     = "Chirurgie",
                Steps =
                [
                    new TreatmentStep { Order=1, Description="Anestezie", DurationMins=10, Cost=80m },
                    new TreatmentStep { Order=2, Description="Extracție",  DurationMins=20, Cost=250m }
                ],
                Equipment = new List<string> { "Forceps", "Elevator" },
                Warnings  = new List<string> { "Verificați alergii" }
            };

            var tmplClone = tmpl.DeepClone();
            Assert("Template DeepClone: ID diferit",
                tmplClone.TemplateId != tmpl.TemplateId);
            Assert("Template DeepClone: Steps copiate",
                tmplClone.Steps.Count == 2);

            // Modificare în clonă nu afectează originalul
            tmplClone.Steps.Add(new TreatmentStep { Order=3, Description="Sutură", DurationMins=15, Cost=120m });
            Assert("Template DeepClone: adăugare pas în clonă nu afectează originalul",
                tmpl.Steps.Count == 2);

            // Test 7: TotalCost calculat corect
            Assert("Template TotalCost = 330",
                tmpl.TotalCost == 330m);

            // Test 8: PrototypeRegistry – clone independente
            var registry = new PrototypeRegistry();
            registry.RegisterRecord("diabetic", original);
            registry.RegisterTemplate("extractie", tmpl);

            var c1 = registry.GetRecordClone("diabetic");
            var c2 = registry.GetRecordClone("diabetic");
            Assert("Registry: două clone au ID-uri diferite",
                c1.RecordId != c2.RecordId);

            c1.Allergies.Clear();
            Assert("Registry: modificare c1 nu afectează c2",
                c2.Allergies.Count == 2);

            // Test 9: Registry – cheie inexistentă aruncă excepție
            try
            {
                registry.GetRecordClone("inexistent");
                Assert("Registry: cheie inexistentă aruncă excepție", false);
            }
            catch (KeyNotFoundException)
            {
                Assert("Registry: cheie inexistentă aruncă excepție", true);
            }

            // Test 10: Version resetată la 1 în DeepClone
            tmpl.Version = 5;
            var tmplClone2 = tmpl.DeepClone();
            Assert("Template DeepClone resetează Version la 1",
                tmplClone2.Version == 1);
        }

        // ══════════════════════════════════════════════════════════════
        // SINGLETON TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunSingletonTests()
        {
            Console.WriteLine("\n── Singleton Tests ──────────────────────────────────");

            // Test 1: ClinicConfiguration – aceeași instanță
            var cfg1 = ClinicConfiguration.Instance;
            var cfg2 = ClinicConfiguration.Instance;
            Assert("ClinicConfig: aceeași instanță (referință identică)",
                ReferenceEquals(cfg1, cfg2));

            // Test 2: Date implicite încărcate
            Assert("ClinicConfig: ClinicName != empty",
                !string.IsNullOrEmpty(cfg1.ClinicName));
            Assert("ClinicConfig: ConsultationBaseFee > 0",
                cfg1.ConsultationBaseFee > 0);

            // Test 3: UpdateBaseFee se reflectă în ambele referințe
            cfg1.UpdateBaseFee(250m);
            Assert("ClinicConfig: modificare prin cfg1 vizibilă în cfg2",
                cfg2.ConsultationBaseFee == 250m);

            // Test 4: UpdateBaseFee negativ aruncă excepție
            try
            {
                cfg1.UpdateBaseFee(-10m);
                Assert("ClinicConfig: tarif negativ aruncă excepție", false);
            }
            catch (ArgumentException)
            {
                Assert("ClinicConfig: tarif negativ aruncă excepție", true);
            }

            // Test 5: UpdateWorkingHours invalid aruncă excepție
            try
            {
                cfg1.UpdateWorkingHours(
                    new TimeSpan(18, 0, 0),
                    new TimeSpan(8, 0, 0)); // close < open
                Assert("ClinicConfig: orar invalid aruncă excepție", false);
            }
            catch (ArgumentException)
            {
                Assert("ClinicConfig: orar invalid aruncă excepție", true);
            }

            // Test 6: AuditLog – aceeași instanță
            var log1 = AuditLog.Instance;
            var log2 = AuditLog.Instance;
            Assert("AuditLog: aceeași instanță (referință identică)",
                ReferenceEquals(log1, log2));

            // Test 7: Log înregistrează intrări
            int before = log1.Count;
            log1.Log("TEST", "Mesaj test 1");
            log1.Log("TEST", "Mesaj test 2");
            Assert("AuditLog: 2 intrări adăugate",
                log2.Count == before + 2);

            // Test 8: GetByModule filtrează corect
            log1.Log("PAYMENT", "Plată procesată");
            var paymentLogs = log1.GetByModule("PAYMENT");
            Assert("AuditLog: GetByModule('PAYMENT') returnează intrările corecte",
                paymentLogs.Any(e => e.Message.Contains("Plată procesată")));

            // Test 9: GetEntries returnează lista completă
            var entries = log1.GetEntries();
            Assert("AuditLog: GetEntries returnează toate intrările",
                entries.Count == log1.Count);

            // Test 10: AuditEntry conține ThreadId
            log1.Log("THREAD", "Test thread id");
            var last = log1.GetEntries().Last();
            Assert("AuditLog: AuditEntry.ThreadId > 0",
                last.ThreadId > 0);
        }

        // ══════════════════════════════════════════════════════════════
        // SINGLETON THREAD-SAFETY TEST
        // ══════════════════════════════════════════════════════════════
        private static void RunSingletonThreadSafetyTest()
        {
            Console.WriteLine("\n── Singleton Thread-Safety Test ─────────────────────");

            var instances = new System.Collections.Concurrent.ConcurrentBag<AuditLog>();
            var threads   = new List<Thread>();

            for (int i = 0; i < 10; i++)
            {
                threads.Add(new Thread(() =>
                {
                    var inst = AuditLog.Instance;
                    inst.Log("THREAD-TEST", $"Mesaj de pe thread {Environment.CurrentManagedThreadId}");
                    instances.Add(inst);
                }));
            }

            threads.ForEach(t => t.Start());
            threads.ForEach(t => t.Join());

            // Toate cele 10 thread-uri trebuie să fi obținut aceeași instanță
            var distinctInstances = instances.Distinct().Count();
            Assert(
                $"AuditLog thread-safe: 10 thread-uri → 1 singură instanță (distincte: {distinctInstances})",
                distinctInstances == 1);

            // Și să fi scris în același jurnal
            var threadLogs = AuditLog.Instance.GetByModule("THREAD-TEST").Count();
            Assert($"AuditLog: toate 10 mesaje scrise în același jurnal (găsite: {threadLogs})",
                threadLogs == 10);
        }

        private static void PrintSummary()
        {
            int total = _passed + _failed;
            Console.WriteLine($"\n{"─",56}");
            Console.WriteLine($"  Rezultat: {_passed}/{total} teste trecute" +
                (_failed > 0 ? $" | {_failed} EȘUATE" : " | Toate OK ✅"));
            Console.WriteLine($"{"─",56}");
        }
    }
}
