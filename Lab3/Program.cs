using DentalClinic.Lab3.Builder;
using DentalClinic.Lab3.Prototype;
using DentalClinic.Lab3.Singleton;
using DentalClinic.Lab3.Tests;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║    SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ      ║");
Console.WriteLine("║    Laborator 3 – Builder · Prototype · Singleton     ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");

// ════════════════════════════════════════════════════════════════════
//  DEMO 1 – BUILDER
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 1 – BUILDER (Plan de Tratament Personalizat)");
Console.WriteLine("══════════════════════════════════════════════════════");

var builder  = new TreatmentPlanBuilder();
var director = new TreatmentPlanDirector(builder);

// 1a. Fluent API manual – pacient cu asigurare
Console.WriteLine("\n[1a] Plan manual (Fluent API) cu asigurare:");
var planManual = builder
    .ForPatient("Ion Popescu")
    .ByDoctor("Dr. Alexandru Munteanu")
    .WithDiagnosis("Carie pe molarul 1 inferior")
    .WithUrgency("Normală")
    .AddProcedure("Anestezie locală",  "Mandibular", 10,  80m)
    .AddProcedure("Obturație compozit","Molar 1",    35, 320m)
    .AddMedication("Ibuprofen", "400mg × 3/zi", 3)
    .ScheduleAppointment(DateTime.Now.AddDays(3).Date.AddHours(10))
    .AddAfterCareNote("Evitați alimente tari 24h.")
    .AddAfterCareNote("Periaj delicat în zona tratată.")
    .WithInsurance("Donaris VIG")
    .WithDiscount(0.10m)
    .WithNotes("Pacient anxios – sedare ușoară recomandată.")
    .Build();
planManual.Print();

// 1b. Director – plan urgență
Console.WriteLine("\n[1b] Plan via Director – URGENȚĂ:");
var planUrgent = director.BuildEmergencyExtractionPlan(
    "Maria Ionescu", "Dr. Elena Codreanu");
planUrgent.Print();

// 1c. Director – igienizare anuală
Console.WriteLine("\n[1c] Plan via Director – Igienizare anuală:");
var planHygiene = director.BuildAnnualHygienePlan(
    "Andrei Rusu", "Dr. Alexandru Munteanu");
planHygiene.Print();

// 1d. Director – ortodontic student cu reducere
Console.WriteLine("\n[1d] Plan via Director – Ortodontic (student, -15%):");
var planOrtho = director.BuildOrthodonticPlan(
    "Cristina Moraru", "Dr. Elena Codreanu", isStudent: true);
planOrtho.Print();

// ════════════════════════════════════════════════════════════════════
//  DEMO 2 – PROTOTYPE
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 2 – PROTOTYPE (Șabloane Reutilizabile)");
Console.WriteLine("══════════════════════════════════════════════════════");

// 2a. Prototip fișă medicală pentru pacienți diabetici
var diabeticTemplate = new PatientRecord
{
    PatientName  = "** TEMPLATE DIABETIC **",
    HasDiabetes  = true,
    BloodType    = "Necunoscut",
    DentistNotes = "Verificare glicemie pre-intervenție. Anestezie fără adrenalină.",
    Allergies    = [ new("Penicilină","Severă") ],
    Medications  = ["Metformin", "Insulină"]
};

Console.WriteLine("\n[2a] Prototip original fișă diabetică:");
diabeticTemplate.Print();

// Deep clone și personalizare pentru pacient real
var patient1Record = diabeticTemplate.DeepClone();
patient1Record.PatientName = "Gheorghe Popa";
patient1Record.Age         = 58;
patient1Record.BloodType   = "A+";
patient1Record.Allergies.Add(new("Latex","Moderată")); // adăugare în clonă

Console.WriteLine("\n[2a] Clonă Deep personalizată (originalul intact):");
patient1Record.Print();
Console.WriteLine($"\n  Original are {diabeticTemplate.Allergies.Count} alergie(i) (nemodificat ✅)");

// Shallow clone – demonstrație comportament diferit
var shallowClone = diabeticTemplate.ShallowClone();
Console.WriteLine($"\n[2a] Shallow clone – same list reference: " +
    $"{ReferenceEquals(shallowClone.Allergies, diabeticTemplate.Allergies)}");

// 2b. Template protocol tratament
var extractionTemplate = new TreatmentTemplate
{
    TemplateName = "Protocol Extracție Simplă",
    Category     = "Chirurgie Orală",
    Description  = "Procedură standard pentru extracție monoradiculară.",
    Steps =
    [
        new TreatmentStep {Order=1,Description="Anestezie topică", DurationMins=3, Cost=0m},
        new TreatmentStep {Order=2,Description="Anestezie injectabilă",DurationMins=5,Cost=80m},
        new TreatmentStep {Order=3,Description="Sindesmotomie",    DurationMins=5, Cost=50m},
        new TreatmentStep {Order=4,Description="Extracție cu forceps",DurationMins=15,Cost=250m},
        new TreatmentStep {Order=5,Description="Hemostază",        DurationMins=5, Cost=30m}
    ],
    Equipment = ["Forceps", "Elevator", "Sindesmotom", "Tăviță sterilă"],
    Warnings  = ["Verificați alergii la anestezic", "Excludeți anticoagulante"]
};

Console.WriteLine("\n[2b] Template protocol extracție:");
extractionTemplate.Print();

// Clonare și adaptare pentru molar de minte
var wisdom = extractionTemplate.DeepClone();
wisdom.TemplateName = "Protocol Extracție Molar de Minte";
wisdom.Steps.Add(new TreatmentStep
    {Order=6, Description="Sutură", DurationMins=10, Cost=100m});
Console.WriteLine("\n[2b] Clonă adaptată pentru molar de minte:");
wisdom.Print();
Console.WriteLine($"\n  Original are {extractionTemplate.Steps.Count} pași (nemodificat ✅)");

// 2c. Registry de prototipuri
var registry = new PrototypeRegistry();
registry.RegisterRecord("diabetic",    diabeticTemplate);
registry.RegisterRecord("pediatric",   new PatientRecord { PatientName="**TEMPLATE PEDIATRIC**",
    DentistNotes="Pacient minor. Necesită acord parinte.", Medications = ["Paracetamol pediatric"]});
registry.RegisterTemplate("extractie", extractionTemplate);

Console.WriteLine($"\n[2c] Registry înregistrat: " +
    $"{string.Join(", ", registry.ListRecordKeys())} (fișe) | " +
    $"{string.Join(", ", registry.ListTemplateKeys())} (template-uri)");

var fromRegistry = registry.GetRecordClone("pediatric");
fromRegistry.PatientName = "Mihai Rusu";
fromRegistry.Age         = 8;
Console.WriteLine("\n[2c] Clonă din registry (pediatric) personalizată:");
fromRegistry.Print();

// ════════════════════════════════════════════════════════════════════
//  DEMO 3 – SINGLETON
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 3 – SINGLETON (Configurare & Jurnal Audit)");
Console.WriteLine("══════════════════════════════════════════════════════");

// 3a. ClinicConfiguration
var config = ClinicConfiguration.Instance;
Console.WriteLine("\n[3a] Configurare clinică (instanță unică):");
config.Print();

config.UpdateBaseFee(220m);
config.UpdateWorkingHours(new TimeSpan(8,0,0), new TimeSpan(19,0,0));
Console.WriteLine($"\n  Tarif actualizat: {ClinicConfiguration.Instance.ConsultationBaseFee:C}");
Console.WriteLine($"  Orar actualizat:  " +
    $"{ClinicConfiguration.Instance.OpeningTime:hh\\:mm} – " +
    $"{ClinicConfiguration.Instance.ClosingTime:hh\\:mm}");

// 3b. AuditLog
Console.WriteLine("\n[3b] Jurnal audit – evenimente:");
var audit = AuditLog.Instance;
audit.Log("APPOINTMENT", "Programare #1 creată pentru Ion Popescu");
audit.Log("PAYMENT",     "Plată 450 MDL procesată (Card)");
audit.Log("TREATMENT",   "Tratament înregistrat: Obturație compozit");
audit.Log("PATIENT",     "Fișă nouă creată pentru Gheorghe Popa");

Console.WriteLine($"\n  Total evenimente în jurnal: {audit.Count}");
Console.WriteLine($"  Instanță 1 == Instanță 2: " +
    $"{ReferenceEquals(audit, AuditLog.Instance)}");

// 3c. Multi-threading demo
Console.WriteLine("\n[3c] Thread-safety – 5 thread-uri scriu simultan în audit:");
var threads = Enumerable.Range(1, 5).Select(i => new Thread(() =>
    AuditLog.Instance.Log("MULTI-THREAD",
        $"Eveniment de pe thread {i}"))).ToList();
threads.ForEach(t => t.Start());
threads.ForEach(t => t.Join());
Console.WriteLine($"  Jurnal complet: {AuditLog.Instance.Count} intrări totale ✅");

// ════════════════════════════════════════════════════════════════════
//  TESTE
// ════════════════════════════════════════════════════════════════════
TestRunner.RunAll();

Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║               DEMO FINALIZAT CU SUCCES               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
