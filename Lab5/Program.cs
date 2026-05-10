using DentalClinic.Lab5.Flyweight;
using DentalClinic.Lab5.Decorator;
using DentalClinic.Lab5.Bridge;
using DentalClinic.Lab5.Proxy;
using DentalClinic.Lab5.Tests;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║    SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ      ║");
Console.WriteLine("║  Lab 5 – Flyweight · Decorator · Bridge · Proxy      ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");

// ════════════════════════════════════════════════════════════════════
//  DEMO 1 – FLYWEIGHT
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 1 – FLYWEIGHT (Materiale Dentare Partajate)");
Console.WriteLine("══════════════════════════════════════════════════════");

var flyFactory = FlyweightDataSeeder.CreateSeededFactory();

Console.WriteLine("\n[1a] Partajarea instanțelor – verificare referință:");
var mat1 = flyFactory.GetMaterial("M001");
var mat2 = flyFactory.GetMaterial("M001");
var mat3 = flyFactory.GetMaterial("M002");
Console.WriteLine($"  mat1 == mat2 (aceeași instanță): {ReferenceEquals(mat1,mat2)} ✅");
Console.WriteLine($"  mat1 == mat3 (instanțe diferite): {ReferenceEquals(mat1,mat3)} ✅");
Console.WriteLine($"  Material 1: {mat1}");
Console.WriteLine($"  Material 2: {mat3}");

Console.WriteLine("\n[1b] Simulare 10.000 înregistrări de tratament:");
const int N = 10_000;
var rand   = new Random(42);
string[] matCodes   = ["M001","M002","M003","M004","M005"];
string[] toothCodes = ["11","16","36","46","55"];

var records = new List<TreatmentRecord>(N);
for (int i = 0; i < N; i++)
{
    var mat   = flyFactory.GetMaterial(matCodes[rand.Next(matCodes.Length)]);
    var tooth = flyFactory.GetToothType(toothCodes[rand.Next(toothCodes.Length)]);
    records.Add(new TreatmentRecord(
        $"R{i:D5}", $"P{rand.Next(1,500):D3}", $"D{rand.Next(1,8)}",
        DateTime.Now.AddDays(-rand.Next(365)),
        rand.NextDouble() * 2, "Notă clinică",
        mat, tooth));
}

Console.WriteLine($"  Primele 3 înregistrări:");
records.Take(3).ToList().ForEach(r => r.Display());
flyFactory.PrintMemoryReport(N);

// ════════════════════════════════════════════════════════════════════
//  DEMO 2 – DECORATOR
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 2 – DECORATOR (Rapoarte de Tratament)");
Console.WriteLine("══════════════════════════════════════════════════════");

var baseReport = new BasicTreatmentReport(
    "Ion Popescu", "Dr. Alexandru Munteanu",
    new DateTime(2024,5,10), "Carie medie M46",
    "Obturație compozit 2 fețe", 380m);

// 2a: Raport de bază
Console.WriteLine("\n[2a] Raport de bază:");
Console.WriteLine(baseReport.Generate());

// 2b: + Antet oficial (pentru secretară)
Console.WriteLine("\n[2b] + ClinicHeaderDecorator (pentru documente oficiale):");
ITreatmentReport official = new ClinicHeaderDecorator(
    baseReport, "DentaCare Clinic SRL",
    "Bd. Ștefan cel Mare 100, Chișinău", "MS-2024-0042");
Console.WriteLine(official.Generate());

// 2c: + TVA + Asigurare (pentru contabilitate)
Console.WriteLine("\n[2c] + Financial + Insurance (pentru contabilitate):");
ITreatmentReport forAccounting = new FinancialDecorator(
    new InsuranceDecorator(baseReport, "Donaris VIG", "POL-9981", 0.60m),
    0.20m, "Card Visa");
Console.WriteLine(forAccounting.Generate());
Console.WriteLine($"\n  GetCost() din perspectiva pacientului: {forAccounting.GetCost():C}");

// 2d: Raport complet stivuit (5 decoratori)
Console.WriteLine("\n[2d] Raport complet: Header + Medical + Financial + Insurance + Watermark:");
ITreatmentReport fullReport =
    new WatermarkDecorator(
        new FinancialDecorator(
            new InsuranceDecorator(
                new MedicalDetailsDecorator(
                    new ClinicHeaderDecorator(baseReport,
                        "DentaCare Clinic SRL","Bd. Ștefan 100","MS-2024-0042"),
                    "K02.1","Lidocaină 2% cu adrenalină",
                    new List<string>{"Niciuna"},"Control la 2 săptămâni"),
                "Donaris VIG","POL-9981",0.60m),
            0.20m,"Card"),
        "CONFIDENȚIAL");

Console.WriteLine(fullReport.Generate());

// ════════════════════════════════════════════════════════════════════
//  DEMO 3 – BRIDGE
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 3 – BRIDGE (Notificări Multi-Canal)");
Console.WriteLine("══════════════════════════════════════════════════════");

// Canale disponibile
IMessageChannel emailCh   = new EmailChannel();
IMessageChannel smsCh     = new SmsChannel();
IMessageChannel pushCh    = new PushChannel();
IMessageChannel whatsAppCh = new WhatsAppChannel();

var apptDate = DateTime.Now.AddDays(1).Date.AddHours(10);

// Context pacient 1 – preferă email
var ctx1 = new NotificationContext
{
    PatientName     = "Ion Popescu",
    PatientContact  = "ion.popescu@email.md",
    DoctorName      = "Dr. Munteanu",
    AppointmentDate = apptDate,
    TreatmentType   = "Detartraj + Airflow",
    Amount          = 350m
};

// Context pacient 2 – număr de telefon
var ctx2 = ctx1 with
{
    PatientName    = "Maria Ionescu",
    PatientContact = "+37379777888"
};

Console.WriteLine("\n[3a] Reminder prin Email:");
new AppointmentReminderNotification(emailCh).Send(ctx1);

Console.WriteLine("\n[3b] Confirmare prin SMS (același tip, alt canal – Bridge!):");
new AppointmentConfirmationNotification(smsCh).Send(ctx2);

Console.WriteLine("\n[3c] Plată restantă prin WhatsApp:");
new PaymentOverdueNotification(whatsAppCh).Send(
    ctx2 with { Amount = 450m });

Console.WriteLine("\n[3d] Follow-up prin Push:");
new FollowUpNotification(pushCh, daysAfter: 3).Send(
    ctx1 with { PatientContact = "token_ion_device_abc" });

Console.WriteLine("\n[3e] Schimb canal la runtime (fără a schimba tipul):");
var reminder = new AppointmentReminderNotification(emailCh);
Console.WriteLine($"  Canal inițial: {reminder.ChannelName}");
reminder.SetChannel(smsCh);
Console.WriteLine($"  Canal după SetChannel: {reminder.ChannelName}");
reminder.Send(ctx2);

Console.WriteLine("\n[3f] MultiChannelNotification (Email + SMS simultan):");
var multiNotif = new MultiChannelNotification(
    new AppointmentConfirmationNotification(emailCh),
    emailCh, smsCh);
multiNotif.Send(ctx2);

// ════════════════════════════════════════════════════════════════════
//  DEMO 4 – PROXY
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 4 – PROXY (Acces Dosar Medical)");
Console.WriteLine("══════════════════════════════════════════════════════");

// 4a: Virtual Proxy (Lazy Loading)
Console.WriteLine("\n[4a] Virtual Proxy – Lazy Loading:");
Console.WriteLine("  Creare proxy (fără acces DB):");
var lazyProxy = new LazyPatientRecordProxy(
    "P001","Ion Popescu","1990-05-15","+37369111222","ion@email.md");
Console.WriteLine($"  IsLoaded = {lazyProxy.IsLoaded}  ← DB neaccesat ✅");
Console.WriteLine("\n  Primul acces la date:");
Console.WriteLine($"  {lazyProxy.GetBasicInfo()}");
Console.WriteLine($"  IsLoaded = {lazyProxy.IsLoaded}  ← Încărcat la cerere ✅");
Console.WriteLine("  Al doilea acces (din cache, fără DB):");
Console.WriteLine($"  {lazyProxy.GetBasicInfo()}");

// 4b: Protection Proxy
Console.WriteLine("\n[4b] Protection Proxy – Control acces pe roluri:");
var realRecord = new RealPatientRecord("P002","Maria Ionescu",
    "1985-08-22","+37379777","maria@email.md");

var users = new[]
{
    new SystemUser { UserId="U1", Name="Ana (Recepționist)", Role=UserRole.Receptionist },
    new SystemUser { UserId="U2", Name="Olga (Asistentă)",   Role=UserRole.Nurse },
    new SystemUser { UserId="U3", Name="Dr. Munteanu",       Role=UserRole.Doctor },
    new SystemUser { UserId="U4", Name="Admin Sistem",       Role=UserRole.Administrator }
};

foreach (var user in users)
{
    var proxy = new ProtectionProxy(realRecord, user);
    Console.WriteLine($"\n  Utilizator: {user}");
    try { proxy.GetBasicInfo();        Console.WriteLine("    GetBasicInfo        ✅"); }
    catch (UnauthorizedAccessException e) { Console.WriteLine($"    GetBasicInfo        ❌ {e.Message[..50]}"); }

    try { proxy.GetMedicalHistory();   Console.WriteLine("    GetMedicalHistory   ✅"); }
    catch (UnauthorizedAccessException) { Console.WriteLine("    GetMedicalHistory   ❌ (acces refuzat)"); }

    try { proxy.GetFinancialSummary(); Console.WriteLine("    GetFinancialSummary ✅"); }
    catch (UnauthorizedAccessException) { Console.WriteLine("    GetFinancialSummary ❌ (acces refuzat)"); }
}

// 4c: Logging Proxy (GDPR Audit)
Console.WriteLine("\n[4c] Logging Proxy – Audit GDPR:");
var logProxy = new LoggingProxy(realRecord, "DR_MUNTEANU");
logProxy.GetBasicInfo();
logProxy.GetMedicalHistory();
logProxy.AddTreatmentNote("Pacient cooperant, recuperare bună.", "DR_MUNTEANU");

Console.WriteLine($"\n  Jurnal de audit ({logProxy.LogCount} intrări):");
foreach (var entry in logProxy.GetAuditLog())
    Console.WriteLine($"  {entry}");

// 4d: Chain – Logging + Protection
Console.WriteLine("\n[4d] Chain: LoggingProxy → ProtectionProxy → RealRecord:");
var receptionist = new SystemUser { UserId="REC", Name="Recepționist", Role=UserRole.Receptionist };
var chainProxy   = new LoggingProxy(new ProtectionProxy(realRecord, receptionist), "REC");

chainProxy.GetBasicInfo();      // permis
try { chainProxy.GetMedicalHistory(); } catch { } // refuzat + logat

Console.WriteLine($"\n  Jurnal chain ({chainProxy.LogCount} intrări, inclusiv access-uri refuzate):");
foreach (var e in chainProxy.GetAuditLog())
    Console.WriteLine($"  {e}");

// ════════════════════════════════════════════════════════════════════
//  TESTE
// ════════════════════════════════════════════════════════════════════
TestRunner.RunAll();

Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║               DEMO FINALIZAT CU SUCCES               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
