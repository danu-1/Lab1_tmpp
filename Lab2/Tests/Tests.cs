// ═══════════════════════════════════════════════════════════════════════
//  TESTE UNITARE – Factory Method & Abstract Factory
//  (Implementate fără framework extern, rulează direct în consolă)
// ═══════════════════════════════════════════════════════════════════════

using DentalClinic.Lab2.FactoryMethod;
using DentalClinic.Lab2.AbstractFactory;

namespace DentalClinic.Lab2.Tests
{
    public static class TestRunner
    {
        private static int _passed = 0;
        private static int _failed = 0;

        // ── Helper assert ──────────────────────────────────────────────
        private static void Assert(string testName, bool condition, string? failMsg = null)
        {
            if (condition)
            {
                Console.WriteLine($"  ✅ PASS | {testName}");
                _passed++;
            }
            else
            {
                Console.WriteLine($"  ❌ FAIL | {testName}" +
                    (failMsg != null ? $" → {failMsg}" : ""));
                _failed++;
            }
        }

        public static void RunAll()
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════╗");
            Console.WriteLine("║              TESTE UNITARE – Laborator 2             ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");

            RunFactoryMethodTests();
            RunAbstractFactoryTests();
            PrintSummary();
        }

        // ══════════════════════════════════════════════════════════════
        // TESTE – Factory Method
        // ══════════════════════════════════════════════════════════════
        private static void RunFactoryMethodTests()
        {
            Console.WriteLine("\n── Factory Method Tests ─────────────────────────────");

            // Test 1: EmailNotificationCreator creează EmailNotification
            var emailCreator = new EmailNotificationCreator();
            var emailNotif   = emailCreator.CreateNotification();
            Assert(
                "EmailCreator produce INotification de tip Email",
                emailNotif is EmailNotification,
                $"Tip primit: {emailNotif.GetType().Name}");

            Assert(
                "EmailNotification.Channel == 'Email'",
                emailNotif.Channel == "Email");

            // Test 2: SmsNotificationCreator creează SmsNotification
            var smsCreator = new SmsNotificationCreator();
            var smsNotif   = smsCreator.CreateNotification();
            Assert(
                "SmsCreator produce INotification de tip SMS",
                smsNotif is SmsNotification);

            Assert(
                "SmsNotification.Channel == 'SMS'",
                smsNotif.Channel == "SMS");

            // Test 3: PushNotificationCreator creează PushNotification
            var pushCreator = new PushNotificationCreator();
            var pushNotif   = pushCreator.CreateNotification();
            Assert(
                "PushCreator produce INotification de tip Push",
                pushNotif is PushNotification);

            // Test 4: PatientNotificationCreator cu canal Email
            var patientCreatorEmail = new PatientNotificationCreator(NotificationChannel.Email);
            var notifEmail = patientCreatorEmail.CreateNotification();
            Assert(
                "PatientCreator(Email) produce EmailNotification",
                notifEmail is EmailNotification);

            // Test 5: PatientNotificationCreator cu canal SMS
            var patientCreatorSms = new PatientNotificationCreator(NotificationChannel.Sms);
            var notifSms = patientCreatorSms.CreateNotification();
            Assert(
                "PatientCreator(SMS) produce SmsNotification",
                notifSms is SmsNotification);

            // Test 6: PatientNotificationCreator cu canal Push
            var patientCreatorPush = new PatientNotificationCreator(NotificationChannel.Push);
            var notifPush = patientCreatorPush.CreateNotification();
            Assert(
                "PatientCreator(Push) produce PushNotification",
                notifPush is PushNotification);

            // Test 7: Metoda NotifyAppointmentConfirmed nu aruncă excepție
            var dt = DateTime.Now.AddDays(2);
            try
            {
                emailCreator.NotifyAppointmentConfirmed(
                    "test@test.com", "Ion Popescu", "Dr. Munteanu", dt);
                Assert("NotifyAppointmentConfirmed rulează fără excepție", true);
            }
            catch (Exception ex)
            {
                Assert("NotifyAppointmentConfirmed rulează fără excepție",
                    false, ex.Message);
            }

            // Test 8: GetDeliveryReport conține datele trimise
            var notifForReport = (EmailNotification)emailCreator.CreateNotification();
            notifForReport.Send("patient@test.com", "Reminder", "Mâine aveți programare.");
            var report = notifForReport.GetDeliveryReport();
            Assert(
                "GetDeliveryReport conține mesajul trimis",
                report.Contains("patient@test.com"));

            // Test 9: Fiecare apel CreateNotification produce o instanță NOUĂ
            var n1 = emailCreator.CreateNotification();
            var n2 = emailCreator.CreateNotification();
            Assert(
                "CreateNotification produce instanțe noi la fiecare apel",
                !ReferenceEquals(n1, n2));

            // Test 10: Polimorfism – creatorul se poate folosi ca tip de bază
            NotificationCreator creator = new SmsNotificationCreator();
            var product = creator.CreateNotification();
            Assert(
                "Polimorfism: NotificationCreator de bază poate produce SMS",
                product is SmsNotification);
        }

        // ══════════════════════════════════════════════════════════════
        // TESTE – Abstract Factory
        // ══════════════════════════════════════════════════════════════
        private static void RunAbstractFactoryTests()
        {
            Console.WriteLine("\n── Abstract Factory Tests ───────────────────────────");

            // Test 1: BasicFactory creează produse de tip Basic
            ITreatmentPackageFactory basicFactory = new BasicPackageFactory();
            var basicPlan    = basicFactory.CreateTreatmentPlan();
            var basicInstr   = basicFactory.CreateInstrumentSet();
            var basicBilling = basicFactory.CreateBillingStrategy();

            Assert("BasicFactory.PackageName == 'Basic Package'",
                basicFactory.PackageName == "Basic Package");
            Assert("BasicFactory produce BasicTreatmentPlan",
                basicPlan is BasicTreatmentPlan);
            Assert("BasicFactory produce BasicInstrumentSet",
                basicInstr is BasicInstrumentSet);
            Assert("BasicFactory produce BasicBillingStrategy",
                basicBilling is BasicBillingStrategy);

            // Test 2: PremiumFactory creează produse de tip Premium
            ITreatmentPackageFactory premiumFactory = new PremiumPackageFactory();
            Assert("PremiumFactory produce PremiumTreatmentPlan",
                premiumFactory.CreateTreatmentPlan() is PremiumTreatmentPlan);
            Assert("PremiumFactory produce PremiumInstrumentSet",
                premiumFactory.CreateInstrumentSet() is PremiumInstrumentSet);
            Assert("PremiumFactory produce PremiumBillingStrategy",
                premiumFactory.CreateBillingStrategy() is PremiumBillingStrategy);

            // Test 3: PediatricFactory creează produse de tip Pediatric
            ITreatmentPackageFactory pedFactory = new PediatricPackageFactory();
            Assert("PediatricFactory produce PediatricTreatmentPlan",
                pedFactory.CreateTreatmentPlan() is PediatricTreatmentPlan);
            Assert("PediatricFactory produce PediatricInstrumentSet",
                pedFactory.CreateInstrumentSet() is PediatricInstrumentSet);

            // Test 4: BasicBilling – calcul preț fără asigurare
            decimal basicPrice = basicBilling.CalculateFinalPrice(2, false);
            Assert(
                $"BasicBilling: 2 vizite fără asigurare = 1000 MDL (got {basicPrice})",
                basicPrice == 1000m);

            // Test 5: BasicBilling – reducere 15% cu asigurare
            decimal basicInsPrice = basicBilling.CalculateFinalPrice(2, true);
            Assert(
                $"BasicBilling: 2 vizite cu asigurare = 850 MDL (got {basicInsPrice})",
                basicInsPrice == 850m);

            // Test 6: PremiumBilling – primele 3 vizite preț întreg
            var premiumBilling = premiumFactory.CreateBillingStrategy();
            decimal premiumPrice3 = premiumBilling.CalculateFinalPrice(3, false);
            Assert(
                $"PremiumBilling: 3 vizite fără asigurare = 3600 MDL (got {premiumPrice3})",
                premiumPrice3 == 3600m);

            // Test 7: PremiumBilling – vizita 4+ cu reducere 20%
            decimal premiumPrice5 = premiumBilling.CalculateFinalPrice(5, false);
            // 3*1200 + 2*960 = 3600 + 1920 = 5520
            Assert(
                $"PremiumBilling: 5 vizite fără asigurare = 5520 MDL (got {premiumPrice5})",
                premiumPrice5 == 5520m);

            // Test 8: PediatricBilling – reducere 20% cu asigurare
            var pedBilling = pedFactory.CreateBillingStrategy();
            decimal pedPrice = pedBilling.CalculateFinalPrice(4, true);
            // 4*300*0.8 = 960
            Assert(
                $"PediatricBilling: 4 vizite cu asigurare = 960 MDL (got {pedPrice})",
                pedPrice == 960m);

            // Test 9: BasicTreatmentPlan – MaxVisits corect
            Assert(
                $"BasicTreatmentPlan.MaxVisits == 3",
                basicPlan.MaxVisits == 3);

            // Test 10: PremiumInstrumentSet include radiografie digitală
            var premiumInstr = premiumFactory.CreateInstrumentSet();
            Assert(
                "PremiumInstrumentSet.IncludesDigitalXRay == true",
                premiumInstr.IncludesDigitalXRay);

            // Test 11: BasicInstrumentSet NU include radiografie digitală
            Assert(
                "BasicInstrumentSet.IncludesDigitalXRay == false",
                !basicInstr.IncludesDigitalXRay);

            // Test 12: TreatmentPackageConsultant funcționează cu orice fabrică
            try
            {
                var consultant = new TreatmentPackageConsultant(basicFactory);
                consultant.PresentPackageToPatient("Ion Popescu", 2, false);
                Assert("TreatmentPackageConsultant rulează fără excepție", true);
            }
            catch (Exception ex)
            {
                Assert("TreatmentPackageConsultant rulează fără excepție",
                    false, ex.Message);
            }

            // Test 13: Familiile nu se amestecă – Basic factory nu produce Premium
            Assert(
                "BasicFactory NU produce PremiumTreatmentPlan",
                basicFactory.CreateTreatmentPlan() is not PremiumTreatmentPlan);

            // Test 14: Describe() nu returnează string gol
            Assert("BasicTreatmentPlan.Describe() nu e gol",
                !string.IsNullOrWhiteSpace(basicPlan.Describe()));
            Assert("BasicInstrumentSet.Describe() nu e gol",
                !string.IsNullOrWhiteSpace(basicInstr.Describe()));
        }

        // ── Sumar final ────────────────────────────────────────────────
        private static void PrintSummary()
        {
            int total = _passed + _failed;
            Console.WriteLine($"\n{'─',56}");
            Console.WriteLine($"  Rezultat: {_passed}/{total} teste trecute" +
                (_failed > 0 ? $" | {_failed} EȘUATE" : " | Toate OK ✅"));
            Console.WriteLine($"{'─',56}");
        }
    }
}
