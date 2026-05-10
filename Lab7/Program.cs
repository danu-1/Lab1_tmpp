using DentalClinic.Lab7.ChainOfResponsibility;
using DentalClinic.Lab7.State;
using DentalClinic.Lab7.Mediator;
using DentalClinic.Lab7.TemplateMethod;
using DentalClinic.Lab7.Visitor;
using DentalClinic.Lab7.Tests;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║    SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ      ║");
Console.WriteLine("║  Lab 7 – CoR · State · Mediator · Template · Visitor ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");

// ════════════════════════════════════════════════════════════════════
//  DEMO 1 – CHAIN OF RESPONSIBILITY
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 1 – CHAIN OF RESPONSIBILITY (Aprobare Tratamente)");
Console.WriteLine("══════════════════════════════════════════════════════");

var chain = ApprovalChainFactory.BuildStandardChain();

var requests = new[]
{
    new TreatmentApprovalRequest
    { PatientName="Ion Popescu",  TreatmentName="Detartraj + Fluorurare",
      EstimatedCost=350m, Priority=TreatmentPriority.Routine,
      RequestedBy="Asistenta Ana" },

    new TreatmentApprovalRequest
    { PatientName="Maria Ionescu",TreatmentName="Obturație molar + RX",
      EstimatedCost=1800m,Priority=TreatmentPriority.Routine,
      HasInsurance=true, RequestedBy="Dr. Munteanu" },

    new TreatmentApprovalRequest
    { PatientName="Andrei Rusu",  TreatmentName="Implant dentar (faza 1)",
      EstimatedCost=5500m,Priority=TreatmentPriority.Routine,
      HasInsurance=true, RequestedBy="Dr. Codreanu" },

    new TreatmentApprovalRequest
    { PatientName="Cristina Pop", TreatmentName="Reabilitare orală completă",
      EstimatedCost=18000m,Priority=TreatmentPriority.Routine,
      RequestedBy="Dr. Munteanu" },

    new TreatmentApprovalRequest
    { PatientName="BLACKLIST",    TreatmentName="Orice",
      EstimatedCost=100m, IsBlacklisted=true, RequestedBy="Necunoscut" },

    new TreatmentApprovalRequest
    { PatientName="Vasile Urgență",TreatmentName="Extracție urgentă",
      EstimatedCost=800m, Priority=TreatmentPriority.Emergency,
      RequestedBy="Urgențe" }
};

foreach (var req in requests)
{
    Console.WriteLine($"\n  ▶ {req}");
    var result = chain.Handle(req);
    Console.WriteLine($"  ◀ {result}");
}

// ════════════════════════════════════════════════════════════════════
//  DEMO 2 – STATE
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 2 – STATE (Ciclul de Viață al Programării)");
Console.WriteLine("══════════════════════════════════════════════════════");

Console.WriteLine("\n[2a] Flux normal: Scheduled → Confirmed → InProgress → Completed");
var appt1 = new AppointmentContext(
    101, "Ion Popescu", "Dr. Munteanu", "Obturație M46",
    DateTime.Now.AddHours(2));
Console.WriteLine($"  Acțiuni disponibile: {appt1.GetAvailableActions()}");
appt1.Confirm();
Console.WriteLine($"  Acțiuni disponibile: {appt1.GetAvailableActions()}");
appt1.Start();
appt1.Complete("Obturație compozit A2, fără complicații.");
appt1.PrintHistory();

Console.WriteLine("\n[2b] Flux cu pauză: InProgress → OnHold → InProgress → Completed");
var appt2 = new AppointmentContext(
    102, "Maria Ionescu", "Dr. Codreanu", "Tratament canal",
    DateTime.Now.AddHours(3));
appt2.Confirm();
appt2.Start();
appt2.Pause("Anestezic insuficient – așteptăm efect");
Console.WriteLine($"  Acțiuni disponibile: {appt2.GetAvailableActions()}");
appt2.Resume();
appt2.Complete("Canal tratat, obturație temporară.");
appt2.PrintHistory();

Console.WriteLine("\n[2c] Flux anulare: Confirmed → Cancelled");
var appt3 = new AppointmentContext(
    103, "Andrei Rusu", "Dr. Munteanu", "Detartraj",
    DateTime.Now.AddDays(1));
appt3.Confirm();
appt3.Cancel("Pacient plecat în deplasare");
appt3.PrintHistory();

Console.WriteLine("\n[2d] Încercare acțiune invalidă (gestionată cu try/catch):");
try { appt1.Cancel("tentativă"); }
catch (InvalidOperationException ex)
{ Console.WriteLine($"  ⚠️  Excepție capturată: {ex.Message}"); }

// ════════════════════════════════════════════════════════════════════
//  DEMO 3 – MEDIATOR
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 3 – MEDIATOR (Hub Coordonare Clinică)");
Console.WriteLine("══════════════════════════════════════════════════════");

var hub        = new ClinicHub();
var reception  = new ReceptionDepartment(hub);
var treatRoom  = new TreatmentRoomDepartment(hub);
var lab        = new LaboratoryDepartment(hub);
var pharmacy   = new PharmacyDepartment(hub);
var accounting = new AccountingDepartment(hub);

Console.WriteLine($"\n  [Hub] {hub.ComponentCount} departamente conectate\n");

Console.WriteLine("[3a] Creare programare:");
reception.CreateAppointment(
    "Ion Popescu", "ion@email.md", "+37369111",
    "Dr. Munteanu", DateTime.Now.AddDays(1).Date.AddHours(10),
    "Detartraj + Obturație");

Console.WriteLine("\n[3b] Pacient sosit:");
reception.PatientArrived("Ion Popescu", "Dr. Munteanu");

Console.WriteLine("\n[3c] Laborator trimite rezultate:");
lab.SendResults("Ion Popescu", "Hemogramă normală, fără contraindicații");

Console.WriteLine("\n[3d] Tratament finalizat:");
treatRoom.CompleteTreatment("Ion Popescu", "Amoxicilina 500mg x3/zi 7 zile", 650m);

Console.WriteLine($"\n  ── Raport final ──────────────────────────────────");
Console.WriteLine($"  Programări create  : {reception.AppointmentsCreated}");
Console.WriteLine($"  Pacienți primiți   : {treatRoom.PatientsReceived}");
Console.WriteLine($"  Analize procesate  : {lab.AnalysesProcessed}");
Console.WriteLine($"  Rețete procesate   : {pharmacy.PrescriptionsProcessed.Count}");
Console.WriteLine($"  Facturi emise      : {accounting.InvoiceCount}");
Console.WriteLine($"  Venituri totale    : {accounting.TotalRevenue:C}");
Console.WriteLine($"  Evenimente în hub  : {hub.EventHistory.Count}");

// ════════════════════════════════════════════════════════════════════
//  DEMO 4 – TEMPLATE METHOD
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 4 – TEMPLATE METHOD (Rapoarte Medicale)");
Console.WriteLine("══════════════════════════════════════════════════════");

var entries = new List<TreatmentEntry>
{
    new("Ion Popescu", "Dr. Munteanu", "Detartraj ultrasonic",
        new DateTime(2024,1,15), 200m, true),
    new("Ion Popescu", "Dr. Munteanu", "Obturație compozit M46",
        new DateTime(2024,3,20), 380m, true),
    new("Ion Popescu", "Dr. Codreanu", "Consultație ortodontică",
        new DateTime(2024,5,10), 300m, true),
    new("Maria Ionescu","Dr. Munteanu", "Extracție M38",
        new DateTime(2024,2,8), 450m, true),
    new("Maria Ionescu","Dr. Codreanu", "Montare aparate fixe",
        new DateTime(2024,4,15), 2500m, true),
};

Console.WriteLine("\n[4a] Raport sumar tratamente per pacient:");
var rpt1 = new TreatmentSummaryReport("Ion Popescu",
    entries.Where(e => e.PatientName == "Ion Popescu").ToList());
Console.WriteLine(rpt1.GenerateReport());

Console.WriteLine("\n[4b] Raport financiar trimestrial:");
var rpt2 = new FinancialReport(
    new DateTime(2024,1,1), new DateTime(2024,6,30), entries);
Console.WriteLine(rpt2.GenerateReport());

Console.WriteLine("\n[4c] Raport performanță medici:");
var stats = new List<DoctorStats>
{
    new("Munteanu", 45, 42, 12400m, 35.5),
    new("Codreanu",  30, 29, 18500m, 52.0),
    new("Rusu",      20, 18,  8200m, 28.0)
};
var rpt3 = new DoctorPerformanceReport(stats, new DateTime(2024, 5, 1));
Console.WriteLine(rpt3.GenerateReport());

// ════════════════════════════════════════════════════════════════════
//  DEMO 5 – VISITOR
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 5 – VISITOR (Operații pe Catalogul de Servicii)");
Console.WriteLine("══════════════════════════════════════════════════════");

// Structura de servicii (elemente ce acceptă vizitatori)
var services = new List<IDentalServiceElement>
{
    new ConsultationService
        { Code="C001", Name="Consultație generală", Price=150m, DurationMins=20 },
    new TreatmentService
        { Code="T001", Name="Obturație compozit 2F", Price=380m,
          IcdCode="K02.1", RequiresAnesthesia=true, InsuranceCoverable=true },
    new TreatmentService
        { Code="T002", Name="Tratament endodontic 3C", Price=950m,
          IcdCode="K04.1", RequiresAnesthesia=true, InsuranceCoverable=true },
    new SurgicalService
        { Code="S001", Name="Extracție simplă", Price=250m,
          RiskLevel=2, AnesthesiaType="Locală", DurationMins=20 },
    new SurgicalService
        { Code="S002", Name="Implant titan faza 1", Price=4500m,
          RiskLevel=4, AnesthesiaType="Locală/Sedare", DurationMins=90 },
    new AestheticService
        { Code="E001", Name="Albire Zoom profesional", Price=1200m,
          SessionsRequired=1, IsInsuranceCovered=false },
    new ServicePackage
    {
        Name="Pachet Igienizare Completă", DiscountRate=0.15m,
        Services = new List<IDentalServiceElement>
        {
            new TreatmentService { Code="H1",Name="Detartraj",Price=200m,InsuranceCoverable=true },
            new TreatmentService { Code="H2",Name="Airflow",Price=150m,InsuranceCoverable=false },
            new ConsultationService { Code="H3",Name="Periaj",Price=80m }
        }
    }
};

// 5a: CostCalculatorVisitor
Console.WriteLine("\n[5a] Cost Calculator (cu reduceri specifice per tip):");
var costV = new CostCalculatorVisitor();
foreach (var s in services) s.Accept(costV);
costV.PrintReport();

// 5b: TaxVisitor
Console.WriteLine("\n[5b] Tax Visitor (TVA diferențiat per categorie):");
var taxV = new TaxVisitor();
foreach (var s in services) s.Accept(taxV);
taxV.PrintReport();

// 5c: ExportCsvVisitor
Console.WriteLine("\n[5c] Export CSV:");
var csvV = new ExportCsvVisitor();
foreach (var s in services) s.Accept(csvV);
Console.WriteLine(csvV.Result);

// 5d: ExportJsonVisitor
Console.WriteLine("\n[5d] Export JSON (primele 200 caractere):");
var jsonV = new ExportJsonVisitor();
foreach (var s in services) s.Accept(jsonV);
string json = jsonV.Result;
Console.WriteLine(json[..Math.Min(json.Length, 300)] + "\n  ...");

// 5e: InsuranceCheckVisitor
Console.WriteLine("\n[5e] Insurance Check Visitor:");
var insV = new InsuranceCheckVisitor();
foreach (var s in services) s.Accept(insV);
insV.PrintReport();

// ════════════════════════════════════════════════════════════════════
//  TESTE
// ════════════════════════════════════════════════════════════════════
TestRunner.RunAll();

Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║     PROIECT COMPLET – TOATE 7 LABORATOARE GATA!      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
