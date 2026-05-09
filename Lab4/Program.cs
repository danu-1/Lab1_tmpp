using DentalClinic.Lab4.Adapter;
using DentalClinic.Lab4.Composite;
using DentalClinic.Lab4.Facade;
using DentalClinic.Lab4.Tests;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║    SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ      ║");
Console.WriteLine("║    Laborator 4 – Adapter · Composite · Façade        ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");

// ════════════════════════════════════════════════════════════════════
//  DEMO 1 – ADAPTER
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 1 – ADAPTER (Gateway-uri de plată unificate)");
Console.WriteLine("══════════════════════════════════════════════════════");

Console.WriteLine("\n  Clinica acceptă plăți prin 3 furnizori cu API-uri diferite.");
Console.WriteLine("  Codul nostru vorbește DOAR cu IPaymentProcessor – indiferent de furnizor.\n");

// 1a. Procesare plată prin MAIB ePay
Console.WriteLine("[1a] Plată prin MAIB ePay:");
IPaymentProcessor maib = PaymentProcessorFactory.Create(PaymentProviderType.Maib);
var rezultatMaib = maib.ProcessPayment("Ion Popescu", 450m, "Obturație compozit M1");
Console.WriteLine($"  → {rezultatMaib}");

// 1b. Procesare plată prin PayNet Moldova
Console.WriteLine("\n[1b] Plată prin PayNet Moldova:");
IPaymentProcessor paynet = PaymentProcessorFactory.Create(PaymentProviderType.PayNet);
var rezultatPayNet = paynet.ProcessPayment("Maria Ionescu", 690m, "Pachet Igienizare");
Console.WriteLine($"  → {rezultatPayNet}");

// 1c. Procesare plată prin Stripe (internațional, MDL→USD intern)
Console.WriteLine("\n[1c] Plată prin Stripe (conversie MDL→USD internă):");
IPaymentProcessor stripe = PaymentProcessorFactory.Create(PaymentProviderType.Stripe);
var rezultatStripe = stripe.ProcessPayment("Andrei Rusu", 850m, "Consultație + Radiografie panoramică");
Console.WriteLine($"  → {rezultatStripe}");

// 1d. Verificare stare și rambursare – același cod pentru toți furnizorii
Console.WriteLine("\n[1d] Verificare stare tranzacție (toți furnizorii, același cod):");
var processors = new List<IPaymentProcessor> { maib, paynet, stripe };
var results    = new List<PaymentResult>     { rezultatMaib, rezultatPayNet, rezultatStripe };
for (int i = 0; i < processors.Count; i++)
{
    var status = processors[i].CheckStatus(results[i].TransactionId);
    Console.WriteLine($"  {processors[i].ProviderName,-18}: {results[i].TransactionId[..Math.Min(20,results[i].TransactionId.Length)]}... → {status}");
}

Console.WriteLine("\n[1e] Rambursare parțială prin MAIB:");
var rambursare = maib.Refund(rezultatMaib.TransactionId, 200m);
Console.WriteLine($"  → {rambursare}");

// ════════════════════════════════════════════════════════════════════
//  DEMO 2 – COMPOSITE
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 2 – COMPOSITE (Ierarhia serviciilor)");
Console.WriteLine("══════════════════════════════════════════════════════");

// 2a. Servicii individuale (Leaf)
Console.WriteLine("\n[2a] Servicii individuale (Leaf nodes):");
var serviciiFrunze = new List<IDentalServiceComponent>
{
    ServiceCatalog.Consultatie(),
    ServiceCatalog.ObturationComposite(),
    ServiceCatalog.AnestezieLocala()
};
foreach (var s in serviciiFrunze) s.Print();
Console.WriteLine($"\n  Total (cod uniform): {serviciiFrunze.Sum(s => s.Price):F2} MDL " +
                  $"| {serviciiFrunze.Sum(s => s.DurationMins)} min");

// 2b. Pachet simplu (Composite)
Console.WriteLine("\n[2b] Pachet Igienizare Completă (Composite cu discount 5%):");
var igienizare = ServiceCatalog.PachetIgienizareCompleta();
igienizare.Print();

// 2c. Pachet Chirurgical
Console.WriteLine("\n[2c] Pachet Extracție Chirurgicală:");
var chirurgical = ServiceCatalog.PachetChirurgicalExtractie();
chirurgical.Print();

// 2d. Pachet Implant Complet – Composite imbricat (3 niveluri)
Console.WriteLine("\n[2d] Pachet Implant Complet (Composite imbricat, discount 10%):");
Console.WriteLine("     Structură: Pachet principal → 3 sub-pachete → servicii individuale\n");
var implant = ServiceCatalog.PachetImplantComplet();
implant.Print();
Console.WriteLine($"\n  Total servicii individuale în ierarhie: " +
                  $"{implant.GetAllLeafServices().Count()}");

// 2e. Tratare uniformă Leaf + Composite
Console.WriteLine("\n[2e] Tratare uniformă Leaf și Composite prin IDentalServiceComponent:");
var cos = new List<IDentalServiceComponent>
{
    ServiceCatalog.Consultatie(),             // Leaf
    ServiceCatalog.PachetIgienizareCompleta() // Composite
};
Console.WriteLine($"  {"Componenta",-42} {"Preț",10}   {"Durată",8}");
Console.WriteLine($"  {"─────────────────────────────────────────",42} {"─────────",10}   {"──────",8}");
foreach (var c in cos)
    Console.WriteLine($"  {c.Name,-42} {c.Price,10:F2}   {c.DurationMins,6} min");
Console.WriteLine($"  {"TOTAL",42} {cos.Sum(c => c.Price),10:F2}   {cos.Sum(c => c.DurationMins),6} min");

// ════════════════════════════════════════════════════════════════════
//  DEMO 3 – FAÇADE
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 3 – FAÇADE (Rezervare programare simplificată)");
Console.WriteLine("══════════════════════════════════════════════════════");

var facade = new AppointmentFacade();

// 3a. Rezervare completă (serviciu simplu + MAIB + avans)
Console.WriteLine("\n[3a] Rezervare programare – serviciu simplu, avans via MAIB:");
var cerere1 = new AppointmentRequest
{
    PatientName     = "Ion Popescu",
    PatientAge      = 42,
    PatientPhone    = "+373 69 123 456",
    PatientEmail    = "ion.popescu@email.md",
    DoctorName      = "Dr. Alexandru Munteanu",
    DoctorSpecialty = "Stomatologie generală",
    DesiredDateTime = DateTime.Now.AddDays(3).Date.AddHours(10),
    Service         = ServiceCatalog.ObturationComposite(),
    PaymentMethod   = PaymentProviderType.Maib,
    PayDepositNow   = true,
    Notes           = "Pacient anxios"
};
var confirmare1 = facade.BookAppointment(cerere1);
confirmare1.Print();

// 3b. Rezervare cu pachet compus + PayNet + fără avans imediat
Console.WriteLine("\n[3b] Rezervare – pachet igienizare, PayNet, fără avans imediat:");
var cerere2 = new AppointmentRequest
{
    PatientName     = "Maria Ionescu",
    PatientAge      = 35,
    PatientPhone    = "+373 79 234 567",
    PatientEmail    = "maria.ionescu@email.md",
    DoctorName      = "Dr. Elena Codreanu",
    DoctorSpecialty = "Igienistă dentară",
    DesiredDateTime = DateTime.Now.AddDays(5).Date.AddHours(11),
    Service         = ServiceCatalog.PachetIgienizareCompleta(),
    PaymentMethod   = PaymentProviderType.PayNet,
    PayDepositNow   = false
};
var confirmare2 = facade.BookAppointment(cerere2);
confirmare2.Print();

// 3c. Rezervare cu pachet implant complet (Composite imbricat) + Stripe
Console.WriteLine("\n[3c] Rezervare – Pachet Implant Complet, Stripe:");
var cerere3 = new AppointmentRequest
{
    PatientName     = "Gheorghe Popa",
    PatientAge      = 52,
    PatientPhone    = "+373 60 345 678",
    PatientEmail    = "gheorghe.popa@email.md",
    DoctorName      = "Dr. Mihai Ionescu",
    DoctorSpecialty = "Implantolog",
    DesiredDateTime = DateTime.Now.AddDays(7).Date.AddHours(9),
    Service         = ServiceCatalog.PachetImplantComplet(),
    PaymentMethod   = PaymentProviderType.Stripe,
    PayDepositNow   = true
};
var confirmare3 = facade.BookAppointment(cerere3);
confirmare3.Print();

// 3d. Rezervare invalidă – demonstrație validare
Console.WriteLine("\n[3d] Tentativă rezervare invalidă (fără nume pacient):");
var cerereInvalida = new AppointmentRequest
{
    PatientName     = "",   // invalid
    PatientAge      = 30,
    PatientPhone    = "+373 60 000 000",
    PatientEmail    = "test@test.md",
    DoctorName      = "Dr. Test",
    DesiredDateTime = DateTime.Now.AddDays(1),
    Service         = ServiceCatalog.Consultatie()
};
var esuata = facade.BookAppointment(cerereInvalida);
esuata.Print();

// 3e. Plată rest de sumă + anulare
Console.WriteLine("\n[3e] Plată rest de sumă la sosirea pacientului:");
if (confirmare1.Success && confirmare1.BalanceDue > 0)
{
    var platRest = facade.ProcessBalancePayment(
        confirmare1.BookingId, confirmare1.PatientName,
        confirmare1.BalanceDue, PaymentProviderType.Maib);
    Console.WriteLine($"  → {platRest}");
}

Console.WriteLine("\n[3f] Anulare programare:");
if (confirmare2.Success)
{
    bool anulata = facade.CancelAppointment(
        confirmare2.BookingId, cerere2.DoctorName,
        cerere2.DesiredDateTime, cerere2.PatientEmail, cerere2.PatientPhone);
    Console.WriteLine($"  Programare #{confirmare2.BookingId} anulată: {(anulata ? "✅" : "❌")}");
}

// ════════════════════════════════════════════════════════════════════
//  TESTE
// ════════════════════════════════════════════════════════════════════
TestRunner.RunAll();

Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║               DEMO FINALIZAT CU SUCCES               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
