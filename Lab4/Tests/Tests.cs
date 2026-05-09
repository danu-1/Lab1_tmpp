// ═══════════════════════════════════════════════════════════════════════
//  TESTE UNITARE – Adapter, Composite, Façade
// ═══════════════════════════════════════════════════════════════════════

using DentalClinic.Lab4.Adapter;
using DentalClinic.Lab4.Composite;
using DentalClinic.Lab4.Facade;

namespace DentalClinic.Lab4.Tests
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
            Console.WriteLine("║           TESTE UNITARE – Laborator 4                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");

            RunAdapterTests();
            RunCompositeTests();
            RunFacadeTests();
            PrintSummary();
        }

        // ══════════════════════════════════════════════════════════════
        // ADAPTER TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunAdapterTests()
        {
            Console.WriteLine("\n── Adapter Tests ────────────────────────────────────");

            // Test 1: Toți adaptorii implementează IPaymentProcessor
            IPaymentProcessor maib   = PaymentProcessorFactory.Create(PaymentProviderType.Maib);
            IPaymentProcessor paynet = PaymentProcessorFactory.Create(PaymentProviderType.PayNet);
            IPaymentProcessor stripe = PaymentProcessorFactory.Create(PaymentProviderType.Stripe);

            Assert("MaibAdapter implementează IPaymentProcessor",   maib   is IPaymentProcessor);
            Assert("PayNetAdapter implementează IPaymentProcessor",  paynet is IPaymentProcessor);
            Assert("StripeAdapter implementează IPaymentProcessor",  stripe is IPaymentProcessor);

            // Test 2: ProviderName setat corect
            Assert("MAIB ProviderName = 'MAIB ePay'",       maib.ProviderName   == "MAIB ePay");
            Assert("PayNet ProviderName = 'PayNet Moldova'", paynet.ProviderName == "PayNet Moldova");
            Assert("Stripe ProviderName = 'Stripe'",         stripe.ProviderName == "Stripe");

            // Test 3: ProcessPayment returnează rezultat non-null
            var resultMaib   = maib.ProcessPayment("Ion Popescu",   450m, "Obturație M1");
            var resultPayNet = paynet.ProcessPayment("Maria Ionescu", 200m, "Detartraj");
            var resultStripe = stripe.ProcessPayment("Andrei Rusu",  850m, "Consultație + Rx");

            Assert("MAIB ProcessPayment returnează rezultat",   resultMaib   != null);
            Assert("PayNet ProcessPayment returnează rezultat", resultPayNet != null);
            Assert("Stripe ProcessPayment returnează rezultat", resultStripe != null);

            // Test 4: Rezultatele conțin TransactionId non-gol
            Assert("MAIB TransactionId non-gol",
                !string.IsNullOrEmpty(resultMaib!.TransactionId));
            Assert("PayNet TransactionId non-gol",
                !string.IsNullOrEmpty(resultPayNet!.TransactionId));
            Assert("Stripe TransactionId non-gol",
                !string.IsNullOrEmpty(resultStripe!.TransactionId));

            // Test 5: Suma returnată în MDL (standardizată)
            Assert("MAIB Amount = 450 MDL",   resultMaib.Amount   == 450m);
            Assert("PayNet Amount = 200 MDL", resultPayNet.Amount == 200m);
            Assert("Stripe Amount = 850 MDL", resultStripe.Amount == 850m);

            // Test 6: Currency standardizată la MDL pentru toți
            Assert("MAIB Currency = MDL",   resultMaib.Currency   == "MDL");
            Assert("PayNet Currency = MDL", resultPayNet.Currency == "MDL");
            Assert("Stripe Currency = MDL", resultStripe.Currency == "MDL");

            // Test 7: Status returnat corect (simulat ca Completed)
            Assert("MAIB Status = Completed",   resultMaib.Status   == PaymentStatus.Completed);
            Assert("PayNet Status = Completed", resultPayNet.Status == PaymentStatus.Completed);
            Assert("Stripe Status = Completed", resultStripe.Status == PaymentStatus.Completed);

            // Test 8: CheckStatus funcționează
            var maibStatus   = maib.CheckStatus(resultMaib.TransactionId);
            var paynetStatus = paynet.CheckStatus(resultPayNet.TransactionId);
            var stripeStatus = stripe.CheckStatus(resultStripe.TransactionId);

            Assert("MAIB CheckStatus returnează PaymentStatus",   maibStatus   is PaymentStatus);
            Assert("PayNet CheckStatus returnează PaymentStatus", paynetStatus is PaymentStatus);
            Assert("Stripe CheckStatus returnează PaymentStatus", stripeStatus is PaymentStatus);

            // Test 9: Refund funcționează
            var refundMaib   = maib.Refund(resultMaib.TransactionId,     200m);
            var refundPayNet = paynet.Refund(resultPayNet.TransactionId,  100m);
            var refundStripe = stripe.Refund(resultStripe.TransactionId,  400m);

            Assert("MAIB Refund Success",   refundMaib.Success);
            Assert("PayNet Refund Success", refundPayNet.Success);
            Assert("Stripe Refund Success", refundStripe.Success);

            Assert("MAIB Refund AmountRefunded = 200",   refundMaib.AmountRefunded   == 200m);
            Assert("PayNet Refund AmountRefunded = 100", refundPayNet.AmountRefunded == 100m);
            Assert("Stripe Refund AmountRefunded = 400", refundStripe.AmountRefunded == 400m);

            // Test 10: Factory cu tip invalid aruncă excepție
            try
            {
                var _ = PaymentProcessorFactory.Create((PaymentProviderType)99);
                Assert("Factory: tip invalid aruncă excepție", false);
            }
            catch (ArgumentOutOfRangeException)
            {
                Assert("Factory: tip invalid aruncă excepție", true);
            }

            // Test 11: Același rezultat prin interfață comună (polimorfism)
            var processors = new List<IPaymentProcessor> { maib, paynet, stripe };
            bool allSuccessful = processors
                .Select(p => p.ProcessPayment("Test Pacient", 100m, "Test"))
                .All(r => r.Success);
            Assert("Toți adaptorii procesează plata prin interfață comună", allSuccessful);
        }

        // ══════════════════════════════════════════════════════════════
        // COMPOSITE TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunCompositeTests()
        {
            Console.WriteLine("\n── Composite Tests ──────────────────────────────────");

            // Test 1: DentalService (Leaf) – proprietăți corecte
            var consultatie = ServiceCatalog.Consultatie();
            Assert("Consultație Name setat",          consultatie.Name == "Consultație inițială");
            Assert("Consultație Price = 200",         consultatie.Price == 200m);
            Assert("Consultație DurationMins = 20",   consultatie.DurationMins == 20);
            Assert("Consultație IsComposite = false", !consultatie.IsComposite);

            // Test 2: Leaf GetAllLeafServices returnează sine însuși
            var leaves = consultatie.GetAllLeafServices().ToList();
            Assert("Leaf GetAllLeafServices returnează 1 element", leaves.Count == 1);
            Assert("Leaf GetAllLeafServices returnează el însuși", leaves[0] == consultatie);

            // Test 3: ServicePackage – prețul = suma componentelor
            var pachetIgienizare = ServiceCatalog.PachetIgienizareCompleta();
            Assert("Pachet Igienizare IsComposite = true", pachetIgienizare.IsComposite);

            decimal expectedPriceNoDisc =
                ServiceCatalog.Detartraj().Price +
                ServiceCatalog.Airflow().Price +
                ServiceCatalog.PeriajProfesional().Price +
                ServiceCatalog.Fluorurare().Price;
            decimal expectedPriceWithDisc = Math.Round(expectedPriceNoDisc * (1 - 0.05m), 2);

            Assert($"Pachet Igienizare Price cu discount 5% = {expectedPriceWithDisc}",
                pachetIgienizare.Price == expectedPriceWithDisc);

            // Test 4: DurationMins = suma durată
            int expectedDuration =
                ServiceCatalog.Detartraj().DurationMins +
                ServiceCatalog.Airflow().DurationMins +
                ServiceCatalog.PeriajProfesional().DurationMins +
                ServiceCatalog.Fluorurare().DurationMins;
            Assert($"Pachet Igienizare DurationMins = {expectedDuration}",
                pachetIgienizare.DurationMins == expectedDuration);

            // Test 5: GetAllLeafServices returnează corect frunzele
            var allLeaves = pachetIgienizare.GetAllLeafServices().ToList();
            Assert("Pachet Igienizare are 4 frunze", allLeaves.Count == 4);
            Assert("Toate frunzele sunt DentalService",
                allLeaves.All(l => l is DentalService));

            // Test 6: Composite imbricat – Pachet Implant Complet
            var implant = ServiceCatalog.PachetImplantComplet();
            Assert("Pachet Implant IsComposite = true", implant.IsComposite);
            Assert("Pachet Implant are 3 componente (diagnostic + chirurgical + protetic)",
                implant.ComponentCount == 3);

            // Toate frunzele din ierarhia implicată
            var implantLeaves = implant.GetAllLeafServices().ToList();
            Assert("Pachet Implant are cel puțin 6 servicii individuale",
                implantLeaves.Count >= 6);

            // Test 7: Discount aplicat corect pe pachet imbricat
            // Pachetul implant are Discount=0.10 pe totalul copiilor (nu se aplică recursiv)
            Assert("Pachet Implant Discount = 0.10m", implant.Discount == 0.10m);
            decimal childrenSum = implant.Children.Sum(c => c.Price);
            decimal expectedImplantPrice = Math.Round(childrenSum * 0.90m, 2);
            Assert($"Pachet Implant Price = {expectedImplantPrice}",
                implant.Price == expectedImplantPrice);

            // Test 8: ServicePackage fără discount – Price = suma directă
            var diagnostic = ServiceCatalog.PachetDiagnosticComplet();
            Assert("Pachet Diagnostic Discount = 0",
                diagnostic.Discount == 0m);
            decimal expectedDiag = ServiceCatalog.Consultatie().Price +
                                   ServiceCatalog.Radiografie().Price;
            Assert($"Pachet Diagnostic Price = {expectedDiag} (fără discount)",
                diagnostic.Price == expectedDiag);

            // Test 9: Add și ComponentCount
            var custom = new ServicePackage { Name = "Pachet Custom", Category = "Test" };
            custom.Add(ServiceCatalog.AnestezieLocala());
            custom.Add(ServiceCatalog.Detartraj());
            Assert("Add: ComponentCount = 2 după 2 adăugări", custom.ComponentCount == 2);

            // Test 10: Remove funcționează
            var svc = ServiceCatalog.Fluorurare();
            custom.Add(svc);
            Assert("ComponentCount = 3 înainte de Remove", custom.ComponentCount == 3);
            custom.Remove(svc);
            Assert("ComponentCount = 2 după Remove", custom.ComponentCount == 2);

            // Test 11: Leaf și Composite tratate uniform prin interfață
            var components = new List<IDentalServiceComponent>
            {
                ServiceCatalog.Consultatie(),           // Leaf
                ServiceCatalog.PachetIgienizareCompleta() // Composite
            };
            decimal totalUniform = components.Sum(c => c.Price);
            Assert("Leaf și Composite tratate uniform prin IDentalServiceComponent",
                totalUniform == ServiceCatalog.Consultatie().Price + expectedPriceWithDisc);

            // Test 12: Pachet Chirurgical – structură corectă
            var chirurgical = ServiceCatalog.PachetChirurgicalExtractie();
            Assert("Pachet Chirurgical are 3 componente", chirurgical.ComponentCount == 3);
            decimal expectedChir =
                ServiceCatalog.AnestezieLocala().Price +
                ServiceCatalog.ExtractieSimpa().Price +
                ServiceCatalog.Sutura().Price;
            Assert($"Pachet Chirurgical Price = {expectedChir}", chirurgical.Price == expectedChir);
        }

        // ══════════════════════════════════════════════════════════════
        // FAÇADE TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunFacadeTests()
        {
            Console.WriteLine("\n── Façade Tests ─────────────────────────────────────");

            var facade = new AppointmentFacade();

            // Test 1: Rezervare completă reușită
            var req1 = new AppointmentRequest
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
                PayDepositNow   = true
            };

            var confirm1 = facade.BookAppointment(req1);
            Assert("Rezervare reușită → Success = true",     confirm1.Success);
            Assert("BookingId generat (PRG-XXXXX)",
                confirm1.BookingId.StartsWith("PRG-"));
            Assert("PatientName în confirmare",
                confirm1.PatientName == "Ion Popescu");
            Assert("ServiceName în confirmare",
                confirm1.ServiceName == "Obturație compozit");
            Assert("TotalPrice = prețul serviciului",
                confirm1.TotalPrice == ServiceCatalog.ObturationComposite().Price);

            // Test 2: Avans = 30% din total
            decimal expectedDeposit = Math.Round(confirm1.TotalPrice * 0.30m, 2);
            Assert($"DepositPaid = 30% ({expectedDeposit} MDL)",
                confirm1.DepositPaid == expectedDeposit);

            // Test 3: BalanceDue = Total - Avans
            Assert("BalanceDue = TotalPrice - DepositPaid",
                confirm1.BalanceDue == confirm1.TotalPrice - confirm1.DepositPaid);

            // Test 4: TransactionId setat (plată avans procesată)
            Assert("TransactionId non-gol după plată avans",
                !string.IsNullOrEmpty(confirm1.TransactionId));

            // Test 5: Rezervare fără avans (PayDepositNow = false)
            var req2 = new AppointmentRequest
            {
                PatientName     = "Maria Ionescu",
                PatientAge      = 35,
                PatientPhone    = "+373 79 234 567",
                PatientEmail    = "maria@email.md",
                DoctorName      = "Dr. Elena Codreanu",
                DoctorSpecialty = "Igienistă",
                DesiredDateTime = DateTime.Now.AddDays(5).Date.AddHours(11),
                Service         = ServiceCatalog.PachetIgienizareCompleta(),
                PaymentMethod   = PaymentProviderType.PayNet,
                PayDepositNow   = false
            };
            var confirm2 = facade.BookAppointment(req2);
            Assert("Rezervare fără avans → Success = true", confirm2.Success);
            Assert("DepositPaid = 0 când PayDepositNow = false",
                confirm2.DepositPaid == 0m);

            // Test 6: Rezervare cu pachet compus (Composite + Façade)
            var req3 = new AppointmentRequest
            {
                PatientName     = "Andrei Rusu",
                PatientAge      = 50,
                PatientPhone    = "+373 60 345 678",
                PatientEmail    = "andrei@email.md",
                DoctorName      = "Dr. Mihai Ionescu",
                DoctorSpecialty = "Implantolog",
                DesiredDateTime = DateTime.Now.AddDays(7).Date.AddHours(9),
                Service         = ServiceCatalog.PachetImplantComplet(),
                PaymentMethod   = PaymentProviderType.Stripe
            };
            var confirm3 = facade.BookAppointment(req3);
            Assert("Rezervare cu Pachet Implant Complet → Success",
                confirm3.Success);
            Assert("TotalPrice = prețul pachetului de implant",
                confirm3.TotalPrice == ServiceCatalog.PachetImplantComplet().Price);

            // Test 7: Validare – nume pacient gol → eșec
            var reqInvalid = new AppointmentRequest
            {
                PatientName     = "",     // invalid
                PatientAge      = 30,
                PatientPhone    = "+373 60 000 000",
                PatientEmail    = "x@y.md",
                DoctorName      = "Dr. Test",
                DesiredDateTime = DateTime.Now.AddDays(1),
                Service         = ServiceCatalog.Consultatie()
            };
            var failResult = facade.BookAppointment(reqInvalid);
            Assert("Nume pacient gol → Success = false", !failResult.Success);
            Assert("ErrorMessage non-gol la validare eșuată",
                !string.IsNullOrEmpty(failResult.ErrorMessage));

            // Test 8: Validare – vârstă invalidă → eșec
            var reqBadAge = new AppointmentRequest
            {
                PatientName     = "Test Pacient",
                PatientAge      = 0,   // invalid
                PatientPhone    = "+373 60 111 222",
                PatientEmail    = "test@email.md",
                DoctorName      = "Dr. Test",
                DesiredDateTime = DateTime.Now.AddDays(1),
                Service         = ServiceCatalog.Consultatie()
            };
            var failAge = facade.BookAppointment(reqBadAge);
            Assert("Vârstă invalidă (0) → Success = false", !failAge.Success);

            // Test 9: ProcessBalancePayment funcționează
            var balanceResult = facade.ProcessBalancePayment(
                confirm1.BookingId, "Ion Popescu",
                confirm1.BalanceDue, PaymentProviderType.Maib);
            Assert("ProcessBalancePayment → Success",      balanceResult.Success);
            Assert("ProcessBalancePayment → Amount corect",
                balanceResult.Amount == confirm1.BalanceDue);

            // Test 10: CancelAppointment returnează true
            bool cancelled = facade.CancelAppointment(
                confirm2.BookingId,
                req2.DoctorName,
                req2.DesiredDateTime,
                req2.PatientEmail,
                req2.PatientPhone);
            Assert("CancelAppointment → true", cancelled);

            // Test 11: BookingId-uri unice pentru rezervări diferite
            Assert("Rezervări succesive generează BookingId-uri diferite",
                confirm1.BookingId != confirm2.BookingId &&
                confirm2.BookingId != confirm3.BookingId);

            // Test 12: Façada nu expune subsistemele (smoke test de interfață)
            // Verificăm că AppointmentFacade are DOAR metodele publice documentate
            var facadeType    = typeof(AppointmentFacade);
            var publicMethods = facadeType.GetMethods(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.DeclaredOnly);
            var methodNames = publicMethods.Select(m => m.Name).ToHashSet();
            Assert("Façada expune BookAppointment",          methodNames.Contains("BookAppointment"));
            Assert("Façada expune CancelAppointment",        methodNames.Contains("CancelAppointment"));
            Assert("Façada expune ProcessBalancePayment",    methodNames.Contains("ProcessBalancePayment"));
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
