// ═══════════════════════════════════════════════════════════════════════
//  TESTE UNITARE – Chain of Responsibility, State, Mediator,
//                  Template Method, Visitor
// ═══════════════════════════════════════════════════════════════════════

using DentalClinic.Lab7.ChainOfResponsibility;
using DentalClinic.Lab7.State;
using DentalClinic.Lab7.Mediator;
using DentalClinic.Lab7.TemplateMethod;
using DentalClinic.Lab7.Visitor;

namespace DentalClinic.Lab7.Tests
{
    public static class TestRunner
    {
        private static int _pass, _fail;

        private static void Assert(string name, bool ok, string? msg = null)
        {
            if (ok) { Console.WriteLine($"  ✅ PASS | {name}"); _pass++; }
            else    { Console.WriteLine($"  ❌ FAIL | {name}{(msg!=null?" → "+msg:"")}"); _fail++; }
        }

        public static void RunAll()
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║           TESTE UNITARE – Laborator 7                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            RunChainTests();
            RunStateTests();
            RunMediatorTests();
            RunTemplateMethodTests();
            RunVisitorTests();
            PrintSummary();
        }

        // ══════════════════════════════════════════════════════════════
        //  CHAIN OF RESPONSIBILITY TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunChainTests()
        {
            Console.WriteLine("\n── Chain of Responsibility Tests ────────────────────");
            var chain = ApprovalChainFactory.BuildStandardChain();

            // T1: Cerere mică aprobată de asistentă
            var r1 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="Ion",TreatmentName="Detartraj",EstimatedCost=300m,
              Priority=TreatmentPriority.Routine,RequestedBy="Nurse" });
            Assert("Cerere ≤500 MDL rutină → aprobată de Asistentă (nivel 1)",
                r1.Approved && r1.HandlerLevel == 1);

            // T2: Cerere medie aprobată de medic
            var r2 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="Maria",TreatmentName="Obturație",EstimatedCost=1500m,
              Priority=TreatmentPriority.Routine,RequestedBy="Doctor" });
            Assert("Cerere ≤2000 MDL → aprobată de Medic (nivel 2)",
                r2.Approved && r2.HandlerLevel == 2);

            // T3: Cerere mare aprobată de șef clinică
            var r3 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="Ana",TreatmentName="Implant",EstimatedCost=5000m,
              RequestedBy="Doctor" });
            Assert("Cerere ≤8000 MDL → aprobată de Șef Clinică (nivel 3)",
                r3.Approved && r3.HandlerLevel == 3);

            // T4: Cerere foarte mare aprobată de director
            var r4 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="Vasile",TreatmentName="Tratament complex",EstimatedCost=15000m,
              RequestedBy="Doctor" });
            Assert("Cerere >8000 MDL → aprobată de Director (nivel 4)",
                r4.Approved && r4.HandlerLevel == 4);

            // T5: Pacient blacklisted → respins imediat
            var r5 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="X",TreatmentName="Orice",EstimatedCost=100m,
              IsBlacklisted=true,RequestedBy="Nurse" });
            Assert("Pacient blacklisted → RESPINS indiferent de sumă",
                !r5.Approved);

            // T6: Documente incomplete → respins
            var r6 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="Y",TreatmentName="Orice",EstimatedCost=200m,
              DocumentsComplete=false,RequestedBy="Nurse" });
            Assert("Documente incomplete → RESPINS",
                !r6.Approved);

            // T7: Cost excesiv → respins de director
            var r7 = chain.Handle(new TreatmentApprovalRequest
            { PatientName="Z",TreatmentName="Imposibil",EstimatedCost=100_000m,
              RequestedBy="Doctor" });
            Assert("Cost >50000 MDL → RESPINS de Director",
                !r7.Approved);

            // T8: Urgență bypasses via lanț de urgență
            var emergChain = ApprovalChainFactory.BuildEmergencyChain();
            var r8 = emergChain.Handle(new TreatmentApprovalRequest
            { PatientName="Urgență",TreatmentName="Extracție urgentă",
              EstimatedCost=9000m,Priority=TreatmentPriority.Emergency,
              RequestedBy="Doctor" });
            Assert("Urgență via EmergencyChain → aprobată",
                r8.Approved);

            // T9: ApprovalResult conține ApprovedBy
            Assert("ApprovalResult.ApprovedBy != empty",
                !string.IsNullOrEmpty(r1.ApprovedBy));

            // T10: ApprovalResult.ProcessedAt setată
            Assert("ApprovalResult.ProcessedAt != default",
                r1.ProcessedAt != default);
        }

        // ══════════════════════════════════════════════════════════════
        //  STATE TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunStateTests()
        {
            Console.WriteLine("\n── State Tests ──────────────────────────────────────");

            // T1: Stare inițială = Scheduled
            var appt = new AppointmentContext(1,"Ion","Dr.X","Detartraj",
                DateTime.Now.AddDays(1));
            Assert("Stare inițială = Scheduled",
                appt.CurrentStateName == "Scheduled");

            // T2: Scheduled → Confirmed
            appt.Confirm();
            Assert("Confirm() → Confirmed",
                appt.CurrentStateName == "Confirmed");

            // T3: Confirmed → InProgress
            appt.Start();
            Assert("Start() → InProgress",
                appt.CurrentStateName == "InProgress");

            // T4: InProgress → OnHold
            appt.Pause("Pacient anxios");
            Assert("Pause() → OnHold",
                appt.CurrentStateName == "OnHold");

            // T5: OnHold → InProgress
            appt.Resume();
            Assert("Resume() → InProgress",
                appt.CurrentStateName == "InProgress");

            // T6: InProgress → Completed
            appt.Complete("Fără complicații");
            Assert("Complete() → Completed",
                appt.CurrentStateName == "Completed");

            // T7: Completed → excepție la orice acțiune
            try { appt.Cancel("test"); Assert("Completed.Cancel() aruncă excepție", false); }
            catch (InvalidOperationException) { Assert("Completed.Cancel() aruncă excepție", true); }

            // T8: Scheduled → Cancel
            var appt2 = new AppointmentContext(2,"Maria","Dr.Y","Obturație",
                DateTime.Now.AddDays(2));
            appt2.Cancel("Pacient anulat");
            Assert("Scheduled → Cancel → Cancelled",
                appt2.CurrentStateName == "Cancelled");

            // T9: Cancelled → excepție la orice acțiune
            try { appt2.Confirm(); Assert("Cancelled.Confirm() aruncă excepție", false); }
            catch (InvalidOperationException) { Assert("Cancelled.Confirm() aruncă excepție", true); }

            // T10: Istoricul conține toate tranzițiile
            Assert("History conține tranziții (≥5 pentru appt1)",
                appt.History.Count >= 5);

            // T11: InProgress nu se poate anula direct
            var appt3 = new AppointmentContext(3,"X","Dr.Z","Test",DateTime.Now.AddDays(1));
            appt3.Confirm(); appt3.Start();
            try { appt3.Cancel("test"); Assert("InProgress.Cancel() aruncă excepție", false); }
            catch (InvalidOperationException) { Assert("InProgress.Cancel() aruncă excepție", true); }

            // T12: GetAvailableActions returnează string non-gol
            var appt4 = new AppointmentContext(4,"T","D","T",DateTime.Now.AddDays(1));
            Assert("GetAvailableActions() != empty",
                !string.IsNullOrEmpty(appt4.GetAvailableActions()));
        }

        // ══════════════════════════════════════════════════════════════
        //  MEDIATOR TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunMediatorTests()
        {
            Console.WriteLine("\n── Mediator Tests ───────────────────────────────────");

            var hub         = new ClinicHub();
            var reception   = new ReceptionDepartment(hub);
            var treatment   = new TreatmentRoomDepartment(hub);
            var lab         = new LaboratoryDepartment(hub);
            var pharmacy    = new PharmacyDepartment(hub);
            var accounting  = new AccountingDepartment(hub);

            // T1: Toate componentele înregistrate
            Assert("5 componente înregistrate în hub",
                hub.ComponentCount == 5);

            // T2: CreateAppointment generează evenimente
            int eventsBefore = hub.EventHistory.Count;
            reception.CreateAppointment("Ion","ion@t.md","+373","Dr.Munt",
                DateTime.Now.AddDays(1),"Detartraj");
            Assert("CreateAppointment generează eveniment în history",
                hub.EventHistory.Count > eventsBefore);

            // T3: AppointmentsCreated incrementat
            Assert("AppointmentsCreated == 1 după Create",
                reception.AppointmentsCreated == 1);

            // T4: PatientArrived trimite la TreatmentRoom
            int logBefore = treatment.EventLog.Count;
            reception.PatientArrived("Ion","Dr.Munt");
            Assert("PatientArrived ajunge la TreatmentRoom",
                treatment.EventLog.Count > logBefore);

            // T5: PatientsReceived incrementat
            Assert("TreatmentRoom.PatientsReceived == 1",
                treatment.PatientsReceived == 1);

            // T6: CompleteTreatment notifică Accounting
            decimal revBefore = accounting.TotalRevenue;
            treatment.CompleteTreatment("Ion","Amoxicilina 500mg",650m);
            Assert("CompleteTreatment → Accounting primește factură",
                accounting.TotalRevenue > revBefore);

            // T7: TotalRevenue corect
            Assert($"TotalRevenue == 650 (got {accounting.TotalRevenue})",
                accounting.TotalRevenue == 650m);

            // T8: Pharmacy primește rețeta
            Assert("Pharmacy procesează rețeta",
                pharmacy.PrescriptionsProcessed.Count == 1);

            // T9: Lab publică rezultate
            lab.SendResults("Ion","Frotiu negativ");
            Assert("Lab publică LAB_RESULTS_READY",
                hub.EventHistory.Any(e => e.EventType == "LAB_RESULTS_READY"));

            // T10: EventHistory crește la fiecare eveniment
            int histBefore = hub.EventHistory.Count;
            reception.CreateAppointment("Maria","m@t.md","+373","Dr.Cod",
                DateTime.Now.AddDays(2),"Obturație");
            Assert("EventHistory crește la fiecare operație",
                hub.EventHistory.Count > histBefore);
        }

        // ══════════════════════════════════════════════════════════════
        //  TEMPLATE METHOD TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunTemplateMethodTests()
        {
            Console.WriteLine("\n── Template Method Tests ────────────────────────────");

            var entries = new List<TreatmentEntry>
            {
                new("Ion Popescu","Dr.Munt","Detartraj",
                    new DateTime(2024,1,15),200m,true),
                new("Ion Popescu","Dr.Munt","Obturatie",
                    new DateTime(2024,3,20),380m,true),
                new("Ion Popescu","Dr.Cod","Consultatie",
                    new DateTime(2024,5,1),150m,false)
            };

            // T1: TreatmentSummaryReport generează string non-gol
            var rpt1 = new TreatmentSummaryReport("Ion Popescu", entries);
            string result1 = rpt1.GenerateReport();
            Assert("TreatmentSummaryReport: raport generat", !string.IsNullOrEmpty(result1));

            // T2: Raportul conține numele pacientului
            Assert("TreatmentSummaryReport: conține PatientName",
                result1.Contains("Ion Popescu"));

            // T3: Raportul conține antet și footer
            Assert("TreatmentSummaryReport: conține 'DentaCare'",
                result1.Contains("DentaCare"));

            // T4: TreatmentSummaryReport: validare – fără intrări → excepție
            try
            {
                var rptEmpty = new TreatmentSummaryReport("X", new List<TreatmentEntry>());
                rptEmpty.GenerateReport();
                Assert("TreatmentSummary: lista goală aruncă excepție", false);
            }
            catch (InvalidOperationException)
            {
                Assert("TreatmentSummary: lista goală aruncă excepție", true);
            }

            // T5: FinancialReport generat corect
            var from = new DateTime(2024, 1, 1);
            var to   = new DateTime(2024, 12, 31);
            var rpt2 = new FinancialReport(from, to, entries);
            string result2 = rpt2.GenerateReport();
            Assert("FinancialReport: raport generat", !string.IsNullOrEmpty(result2));

            // T6: FinancialReport conține TVA
            Assert("FinancialReport: conține 'TVA'", result2.Contains("TVA"));

            // T7: FinancialReport: validare perioadă invalidă
            try
            {
                var rptInvalid = new FinancialReport(
                    new DateTime(2024,12,31), new DateTime(2024,1,1), entries);
                rptInvalid.GenerateReport();
                Assert("FinancialReport: perioadă inversă aruncă excepție", false);
            }
            catch (InvalidOperationException)
            {
                Assert("FinancialReport: perioadă inversă aruncă excepție", true);
            }

            // T8: DoctorPerformanceReport generat
            var stats = new List<DoctorStats>
            {
                new("Munteanu", 20, 18, 5400m, 35.0),
                new("Codreanu",  15, 15, 9000m, 45.0)
            };
            var rpt3 = new DoctorPerformanceReport(stats, new DateTime(2024, 5, 1));
            string result3 = rpt3.GenerateReport();
            Assert("DoctorPerformanceReport: raport generat", !string.IsNullOrEmpty(result3));

            // T9: Raport conține TOP PERFORMER
            Assert("DoctorPerformanceReport: conține TOP PERFORMER",
                result3.Contains("TOP PERFORMER"));

            // T10: Template Method sealed – GenerateReport nu poate fi suprascrisa
            Assert("GenerateReport este sealed (nu poate fi suprascrisă)",
                typeof(MedicalReportGenerator)
                    .GetMethod("GenerateReport")!
                    .IsFinal);
        }

        // ══════════════════════════════════════════════════════════════
        //  VISITOR TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunVisitorTests()
        {
            Console.WriteLine("\n── Visitor Tests ────────────────────────────────────");

            // Date de test
            var consult = new ConsultationService
                { Code="C001", Name="Consultație", Price=150m, DurationMins=20 };
            var treatment = new TreatmentService
                { Code="T001", Name="Obturație", Price=380m, IcdCode="K02.1",
                  InsuranceCoverable=true, RequiresAnesthesia=true };
            var surgery = new SurgicalService
                { Code="S001", Name="Extracție", Price=250m, RiskLevel=2,
                  AnesthesiaType="Locală", DurationMins=20 };
            var aesthetic = new AestheticService
                { Code="E001", Name="Albire", Price=1200m, SessionsRequired=3,
                  IsInsuranceCovered=false };
            var pkg = new ServicePackage
            {
                Name="Pachet Igienizare", DiscountRate=0.10m,
                Services = new List<IDentalServiceElement> { consult, treatment }
            };

            // T1: CostCalculatorVisitor – consultație cu -10%
            var costV = new CostCalculatorVisitor();
            consult.Accept(costV);
            decimal expectedConsult = 150m * 0.90m;
            Assert($"CostVisitor: consultație -{10}% = {expectedConsult} (got {costV.TotalCost})",
                costV.TotalCost == expectedConsult);

            // T2: TreatmentService cu asigurare -30%
            var costV2 = new CostCalculatorVisitor();
            treatment.Accept(costV2);
            decimal expectedTreatment = 380m * 0.70m;
            Assert($"CostVisitor: tratament asig -30% = {expectedTreatment} (got {costV2.TotalCost})",
                costV2.TotalCost == expectedTreatment);

            // T3: SurgicalService risc<4 – fără suprataxă
            var costV3 = new CostCalculatorVisitor();
            surgery.Accept(costV3);
            Assert($"CostVisitor: chirurgie risc2 = {surgery.Price} (got {costV3.TotalCost})",
                costV3.TotalCost == surgery.Price);

            // T4: AestheticService – × sesiuni
            var costV4 = new CostCalculatorVisitor();
            aesthetic.Accept(costV4);
            decimal expectedAesthetic = 1200m * 3;
            Assert($"CostVisitor: estetică 3 sesiuni = {expectedAesthetic} (got {costV4.TotalCost})",
                costV4.TotalCost == expectedAesthetic);

            // T5: TaxVisitor – TVA estetică 20%
            var taxV = new TaxVisitor();
            aesthetic.Accept(taxV);
            decimal expectedTax = Math.Round(1200m * 3 * 0.20m, 2);
            Assert($"TaxVisitor: TVA estetică = {expectedTax} (got {taxV.TotalTax})",
                taxV.TotalTax == expectedTax);

            // T6: ExportCsvVisitor – CSV conține header
            var csvV = new ExportCsvVisitor();
            consult.Accept(csvV);
            treatment.Accept(csvV);
            Assert("CsvVisitor: CSV conține header 'Categorie'",
                csvV.Result.Contains("Categorie"));
            Assert("CsvVisitor: CSV conține consultație",
                csvV.Result.Contains("C001"));
            Assert("CsvVisitor: CSV conține tratament",
                csvV.Result.Contains("T001"));

            // T7: ExportJsonVisitor – JSON valid (conține brackets)
            var jsonV = new ExportJsonVisitor();
            consult.Accept(jsonV);
            surgery.Accept(jsonV);
            Assert("JsonVisitor: Result conține '['", jsonV.Result.Contains("["));
            Assert("JsonVisitor: Result conține 'category'",
                jsonV.Result.Contains("category"));

            // T8: InsuranceCheckVisitor – tratament acoperit
            var insV = new InsuranceCheckVisitor();
            consult.Accept(insV);
            treatment.Accept(insV);
            aesthetic.Accept(insV);
            Assert("InsuranceVisitor: consultație în CoveredServices",
                insV.CoveredServices.Any(s => s.Contains("Consultație")));
            Assert("InsuranceVisitor: estetică în UncoveredServices",
                insV.UncoveredServices.Any(s => s.Contains("Albire")));

            // T9: ServicePackage traversează recursiv
            var costPkg = new CostCalculatorVisitor();
            pkg.Accept(costPkg);
            // Pachet apelează Visit(pkg) + Visit(consult) + Visit(treatment)
            Assert("ServicePackage.Accept traversează recursiv copiii",
                costPkg.Breakdown.Count >= 2);

            // T10: Double dispatch – tipul concret corect
            var taxV2 = new TaxVisitor();
            consult.Accept(taxV2);
            aesthetic.Accept(taxV2);
            // TVA consultație 8% + estetică 20% – ambele vizitate
            Assert("TaxVisitor: dispatch corect pe Consultation + Aesthetic",
                taxV2.TotalTax > 0);
        }

        private static void PrintSummary()
        {
            int total = _pass + _fail;
            Console.WriteLine($"\n{"─",56}");
            Console.WriteLine($"  Rezultat: {_pass}/{total} teste trecute" +
                (_fail > 0 ? $" | {_fail} EȘUATE" : " | Toate OK ✅"));
            Console.WriteLine($"{"─",56}");
        }
    }
}
