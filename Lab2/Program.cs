using DentalClinic.Lab2.FactoryMethod;
using DentalClinic.Lab2.AbstractFactory;
using DentalClinic.Lab2.Tests;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║    SISTEM DE MANAGEMENT - CLINICĂ STOMATOLOGICĂ      ║");
Console.WriteLine("║       Laborator 2 – Factory Method & Abstract Factory ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");

// ════════════════════════════════════════════════════════════════════
//  DEMO 1 – FACTORY METHOD
//  Scenariul: clinica trimite notificări pacienților.
//  Fiecare pacient are un canal preferat stocat în profil.
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 1 – FACTORY METHOD (Sistem de Notificări)");
Console.WriteLine("══════════════════════════════════════════════════════");

var appointmentTime = DateTime.Now.AddDays(3).Date.AddHours(10);

// Pacient 1: preferă Email
Console.WriteLine("\n[Pacient 1 – canal preferat: Email]");
NotificationCreator emailCreator = new EmailNotificationCreator();
emailCreator.NotifyAppointmentConfirmed(
    "ion.popescu@email.md", "Ion Popescu", "Dr. Munteanu", appointmentTime);
emailCreator.NotifyAppointmentReminder(
    "ion.popescu@email.md", "Ion Popescu", appointmentTime);

// Pacient 2: preferă SMS
Console.WriteLine("\n[Pacient 2 – canal preferat: SMS]");
NotificationCreator smsCreator = new SmsNotificationCreator();
smsCreator.NotifyAppointmentConfirmed(
    "+373 79 777 888", "Maria Ionescu", "Dr. Codreanu", appointmentTime.AddHours(2));
smsCreator.NotifyPaymentOverdue("+373 79 777 888", "Maria Ionescu", 450m);

// Pacient 3: preferă Push (canal determinat dinamic din profil)
Console.WriteLine("\n[Pacient 3 – canal determinat dinamic din profil: Push]");
NotificationCreator dynamicCreator =
    new PatientNotificationCreator(NotificationChannel.Push);
dynamicCreator.NotifyAppointmentReminder(
    "device_token_abc123", "Andrei Rusu", appointmentTime.AddDays(1));

// Demonstrare polimorfism: același cod, canale diferite
Console.WriteLine("\n[Polimorfism – același cod client, 3 canale diferite]");
NotificationCreator[] creators =
[
    new EmailNotificationCreator(),
    new SmsNotificationCreator(),
    new PushNotificationCreator()
];

foreach (var creator in creators)
{
    // Codul client nu știe ce tip concret e folosit
    INotification notif = creator.CreateNotification();
    notif.Send("pacient@clinic.md", "Test", $"Mesaj trimis via {notif.Channel}");
}

// ════════════════════════════════════════════════════════════════════
//  DEMO 2 – ABSTRACT FACTORY
//  Scenariul: consultantul prezintă pachete de tratament pacienților.
//  Fiecare pachet e o familie completă de obiecte compatibile.
// ════════════════════════════════════════════════════════════════════
Console.WriteLine("\n══════════════════════════════════════════════════════");
Console.WriteLine("  DEMO 2 – ABSTRACT FACTORY (Pachete de Tratament)");
Console.WriteLine("══════════════════════════════════════════════════════");

// Selectăm fabrica în funcție de tipul pacientului
// (într-o aplicație reală: din configurare sau input utilizator)

ITreatmentPackageFactory[] factories =
[
    new BasicPackageFactory(),
    new PremiumPackageFactory(),
    new PediatricPackageFactory()
];

string[] patients    = ["Ion Popescu",  "Maria Ionescu", "Mihai Rusu (copil)"];
int[]    visits      = [2,               5,               4];
bool[]   insurances  = [false,           true,            true];

for (int i = 0; i < factories.Length; i++)
{
    // Clientul (consultant) lucrează DOAR cu ITreatmentPackageFactory
    // Nu știe nimic despre BasicPackageFactory sau PremiumPackageFactory
    var consultant = new TreatmentPackageConsultant(factories[i]);
    consultant.PresentPackageToPatient(patients[i], visits[i], insurances[i]);
}

// ════════════════════════════════════════════════════════════════════
//  TESTE UNITARE
// ════════════════════════════════════════════════════════════════════
TestRunner.RunAll();

Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║               DEMO FINALIZAT CU SUCCES               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
