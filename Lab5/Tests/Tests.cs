// ═══════════════════════════════════════════════════════════════════════
//  TESTE UNITARE – Flyweight, Decorator, Bridge, Proxy
// ═══════════════════════════════════════════════════════════════════════

using DentalClinic.Lab5.Flyweight;
using DentalClinic.Lab5.Decorator;
using DentalClinic.Lab5.Bridge;
using DentalClinic.Lab5.Proxy;

namespace DentalClinic.Lab5.Tests
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
            Console.WriteLine("║           TESTE UNITARE – Laborator 5                ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════╝");
            RunFlyweightTests();
            RunDecoratorTests();
            RunBridgeTests();
            RunProxyTests();
            PrintSummary();
        }

        // ══════════════════════════════════════════════════════════════
        //  FLYWEIGHT TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunFlyweightTests()
        {
            Console.WriteLine("\n── Flyweight Tests ──────────────────────────────────");
            var factory = FlyweightDataSeeder.CreateSeededFactory();

            // T1: GetMaterial returnează aceeași instanță la apeluri repetate
            var m1 = factory.GetMaterial("M001");
            var m2 = factory.GetMaterial("M001");
            Assert("GetMaterial returnează aceeași instanță (referință identică)",
                ReferenceEquals(m1, m2));

            // T2: Materiale diferite → instanțe diferite
            var m3 = factory.GetMaterial("M002");
            Assert("Materiale diferite → instanțe diferite",
                !ReferenceEquals(m1, m3));

            // T3: Proprietăți intrinseci corecte
            Assert("M001: MaterialCode == 'M001'",       m1.MaterialCode == "M001");
            Assert("M001: Category == 'Compozit nano'",  m1.Category == "Compozit nano");
            Assert("M001: Shade == 'A2'",                m1.Shade == "A2");

            // T4: CalculateCost corect (0.5g × 10 × 12.50 = 62.50)
            decimal cost = m1.CalculateCost(0.5);
            Assert($"M001 CalculateCost(0.5g) = 62.50 (got {cost})",
                cost == 62.50m);

            // T5: ToothType – aceeași instanță
            var t1 = factory.GetToothType("16");
            var t2 = factory.GetToothType("16");
            Assert("GetToothType returnează aceeași instanță", ReferenceEquals(t1, t2));

            // T6: Proprietăți tooth corecte
            Assert("Molar 16: NumberOfRoots == 3", t1.NumberOfRoots == 3);
            Assert("Molar 16: IsDeciduous == false", !t1.IsDeciduous);

            // T7: Dinte de lapte
            var t55 = factory.GetToothType("55");
            Assert("Dinte 55: IsDeciduous == true", t55.IsDeciduous);

            // T8: Cheie inexistentă → excepție
            try {
                factory.GetMaterial("NOPE");
                Assert("Material inexistent → excepție", false);
            } catch (KeyNotFoundException) {
                Assert("Material inexistent → excepție", true);
            }

            // T9: Număr instanțe unice corect
            Assert("5 materiale unice înregistrate", factory.UniqueMatCount == 5);
            Assert("5 tipuri dinte unice înregistrate", factory.UniqueToothCount == 5);

            // T10: TreatmentRecord folosește flyweights (referințe, nu copii)
            var mat  = factory.GetMaterial("M002");
            var tooth = factory.GetToothType("36");
            var rec1  = new TreatmentRecord("R001","P1","D1",DateTime.Now,0.3,
                "nota",mat,tooth);
            var rec2  = new TreatmentRecord("R002","P2","D1",DateTime.Now,0.5,
                "nota",factory.GetMaterial("M002"), factory.GetToothType("36"));
            // Cost diferit (cantitate diferită) dar aceeași instanță flyweight
            Assert("Record 1 cost != Record 2 cost (cantitate diferită)",
                rec1.GetTreatmentCost() != rec2.GetTreatmentCost());

            Console.WriteLine(); // blank pentru lizibilitate
        }

        // ══════════════════════════════════════════════════════════════
        //  DECORATOR TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunDecoratorTests()
        {
            Console.WriteLine("\n── Decorator Tests ──────────────────────────────────");

            var base_ = new BasicTreatmentReport(
                "Ion Popescu", "Dr. Munteanu",
                new DateTime(2024,5,10), "Carie M46",
                "Obturație compozit", 350m);

            // T1: Raport de bază generat
            string baseRep = base_.Generate();
            Assert("BasicReport conține numele pacientului",
                baseRep.Contains("Ion Popescu"));
            Assert("BasicReport.GetCost == 350",
                base_.GetCost() == 350m);

            // T2: ClinicHeaderDecorator adaugă antet
            var withHeader = new ClinicHeaderDecorator(base_,
                "DentaCare Clinic", "Bd. Ștefan 100", "MS-2024-0042");
            string hRep = withHeader.Generate();
            Assert("ClinicHeader: raport conține numele clinicii",
                hRep.Contains("DentaCare Clinic"));
            Assert("ClinicHeader: raport conține semnătură",
                hRep.Contains("Semnătura"));
            Assert("ClinicHeader.GetCost nedschimbat",
                withHeader.GetCost() == 350m);

            // T3: FinancialDecorator calculează TVA corect
            var withFinance = new FinancialDecorator(base_, 0.20m, "Card");
            Assert("FinancialDecorator.GetCost = 350*1.2 = 420",
                withFinance.GetCost() == 420m);
            string fRep = withFinance.Generate();
            Assert("FinancialDecorator: raport conține TVA",
                fRep.Contains("TVA"));

            // T4: MedicalDecorator adaugă cod ICD
            var withMedical = new MedicalDetailsDecorator(base_,
                "K02.9", "Lidocaină 2%", null, "Control la 2 săptămâni");
            string mRep = withMedical.Generate();
            Assert("MedicalDecorator: raport conține cod ICD-10",
                mRep.Contains("K02.9"));
            Assert("MedicalDecorator: raport conține anestezie",
                mRep.Contains("Lidocaină"));

            // T5: WatermarkDecorator adaugă watermark
            var withWm = new WatermarkDecorator(base_, "CONFIDENȚIAL");
            string wRep = withWm.Generate();
            Assert("WatermarkDecorator: raport conține CONFIDENȚIAL",
                wRep.Contains("CONFIDENȚIAL"));
            Assert("WatermarkDecorator.Title conține CONFIDENȚIAL",
                withWm.Title.Contains("CONFIDENȚIAL"));

            // T6: InsuranceDecorator calculează costul pacientului
            var withIns = new InsuranceDecorator(base_,
                "Donaris VIG", "POL-123", 0.70m);
            decimal patientCost = withIns.GetCost();
            Assert($"InsuranceDecorator: cost pacient = 350*0.30 = 105 (got {patientCost})",
                patientCost == 105m);

            // T7: Stivuire decoratori – ordinea contează
            ITreatmentReport stacked = new WatermarkDecorator(
                new FinancialDecorator(
                    new ClinicHeaderDecorator(base_,
                        "DentaCare", "Adresa", "LIC"), 0.20m), "COPIE");
            string sRep = stacked.Generate();
            Assert("Stivuire 3 decoratori: raport conține antet + TVA + watermark",
                sRep.Contains("DentaCare") &&
                sRep.Contains("TVA") &&
                sRep.Contains("COPIE"));

            // T8: Fiecare decorator adaugă conținut propriu față de wrapped
            Assert("Decorator adaugă conținut (header mai lung decât baza)",
                withHeader.Generate().Length > base_.Generate().Length);

            // T9: Decorator nu modifică obiectul de bază
            string baseAgain = base_.Generate();
            Assert("Baza neschimbată după decorare",
                baseAgain.Contains("Ion Popescu") && !baseAgain.Contains("TVA"));

            // T10: Title propagat și modificat în lanț
            Assert("Title propagat prin decoratori",
                withHeader.Title.Contains("Raport Tratament"));
        }

        // ══════════════════════════════════════════════════════════════
        //  BRIDGE TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunBridgeTests()
        {
            Console.WriteLine("\n── Bridge Tests ─────────────────────────────────────");

            var ctx = new NotificationContext
            {
                PatientName    = "Maria Ionescu",
                PatientContact = "maria@test.md",
                DoctorName     = "Dr. Munteanu",
                AppointmentDate = DateTime.Now.AddDays(1),
                TreatmentType  = "Detartraj",
                Amount         = 400m
            };

            // T1: Email channel disponibil pentru email valid
            var email = new EmailChannel();
            Assert("EmailChannel.IsAvailable pentru email valid",
                email.IsAvailable("test@test.com"));
            Assert("EmailChannel.IsAvailable = false fără @",
                !email.IsAvailable("notanemail"));

            // T2: SMS channel disponibil pentru număr valid
            var sms = new SmsChannel();
            Assert("SmsChannel.IsAvailable pentru nr cu '+'",
                sms.IsAvailable("+37369111222"));
            Assert("SmsChannel.IsAvailable = false pentru email",
                !sms.IsAvailable("test@test.md"));

            // T3: Push channel disponibil pentru token valid
            var push = new PushChannel();
            Assert("PushChannel.IsAvailable pentru token_xxx",
                push.IsAvailable("token_abc123"));
            Assert("PushChannel.IsAvailable = false pentru email",
                !push.IsAvailable("test@test.md"));

            // T4: GetMaxLength diferit pe fiecare canal
            Assert("Email max > SMS max",
                email.GetMaxLength() > sms.GetMaxLength());
            Assert("SMS max == 160",
                sms.GetMaxLength() == 160);

            // T5: AppointmentReminder funcționează cu Email
            var reminderEmail = new AppointmentReminderNotification(email);
            Assert("AppointmentReminder ChannelName == 'Email'",
                reminderEmail.ChannelName == "Email");

            // T6: Schimb canal fără a schimba tipul notificării (Bridge)
            reminderEmail.SetChannel(sms);
            Assert("SetChannel: canal schimbat în Email→SMS",
                reminderEmail.ChannelName == "SMS");
            reminderEmail.SetChannel(email); // restore

            // T7: Send nu aruncă excepție pentru contact compatibil
            try {
                reminderEmail.Send(ctx);
                Assert("AppointmentReminder.Send(email) fără excepție", true);
            } catch {
                Assert("AppointmentReminder.Send(email) fără excepție", false);
            }

            // T8: Send pe canal incompatibil (SMS pentru email contact) – nu aruncă excepție
            var smsNotif = new AppointmentReminderNotification(new SmsChannel());
            try {
                smsNotif.Send(ctx); // contact e email, nu număr – IsAvailable=false
                Assert("Send pe canal incompatibil gestionat fără excepție", true);
            } catch {
                Assert("Send pe canal incompatibil gestionat fără excepție", false);
            }

            // T9: PaymentOverdueNotification diferit față de Reminder
            var payCtx = ctx with { PatientContact = "+37369000111", Amount = 350m };
            var payNotif = new PaymentOverdueNotification(new SmsChannel());
            try {
                payNotif.Send(payCtx);
                Assert("PaymentOverdueNotification.Send funcțional", true);
            } catch {
                Assert("PaymentOverdueNotification.Send funcțional", false);
            }

            // T10: FollowUpNotification funcțional
            var followCtx = ctx with { PatientContact = "token_xyz789" };
            var follow = new FollowUpNotification(new PushChannel(), daysAfter: 3);
            try {
                follow.Send(followCtx);
                Assert("FollowUpNotification.Send funcțional", true);
            } catch {
                Assert("FollowUpNotification.Send funcțional", false);
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  PROXY TESTS
        // ══════════════════════════════════════════════════════════════
        private static void RunProxyTests()
        {
            Console.WriteLine("\n── Proxy Tests ──────────────────────────────────────");

            // ── Virtual Proxy (Lazy) ───────────────────────────────────
            var lazy = new LazyPatientRecordProxy(
                "P001","Ion Popescu","1990-05-15","+37369111","ion@test.md");

            // T1: Înainte de primul acces – nu e încărcat
            Assert("LazyProxy: IsLoaded == false la creare", !lazy.IsLoaded);

            // T2: După primul acces – e încărcat
            _ = lazy.GetBasicInfo();
            Assert("LazyProxy: IsLoaded == true după GetBasicInfo", lazy.IsLoaded);

            // T3: Al doilea acces nu reîncarcă (aceeași instanță)
            string info1 = lazy.GetBasicInfo();
            string info2 = lazy.GetBasicInfo();
            Assert("LazyProxy: date consistente la accese repetate", info1 == info2);

            // ── Protection Proxy ───────────────────────────────────────
            var real = new RealPatientRecord("P002","Maria Ionescu",
                "1985-08-22","+37369222","maria@test.md");

            var receptionist = new SystemUser
                { UserId="U1", Name="Ana Secretară", Role=UserRole.Receptionist };
            var doctor = new SystemUser
                { UserId="U2", Name="Dr. Munteanu", Role=UserRole.Doctor };
            var admin = new SystemUser
                { UserId="U3", Name="Admin", Role=UserRole.Administrator };

            var proxyReceptionist = new ProtectionProxy(real, receptionist);
            var proxyDoctor       = new ProtectionProxy(real, doctor);
            var proxyAdmin        = new ProtectionProxy(real, admin);

            // T4: Recepționist poate GetBasicInfo
            try {
                proxyReceptionist.GetBasicInfo();
                Assert("ProtectionProxy: Receptionist poate GetBasicInfo", true);
            } catch (UnauthorizedAccessException) {
                Assert("ProtectionProxy: Receptionist poate GetBasicInfo", false);
            }

            // T5: Recepționist NU poate GetMedicalHistory
            try {
                proxyReceptionist.GetMedicalHistory();
                Assert("ProtectionProxy: Receptionist REFUZAT la GetMedicalHistory", false);
            } catch (UnauthorizedAccessException) {
                Assert("ProtectionProxy: Receptionist REFUZAT la GetMedicalHistory", true);
            }

            // T6: Doctor poate AddTreatmentNote
            try {
                proxyDoctor.AddTreatmentNote("Consultație de control", "U2");
                Assert("ProtectionProxy: Doctor poate AddTreatmentNote", true);
            } catch (UnauthorizedAccessException) {
                Assert("ProtectionProxy: Doctor poate AddTreatmentNote", false);
            }

            // T7: Doctor NU poate GetFinancialSummary
            try {
                proxyDoctor.GetFinancialSummary();
                Assert("ProtectionProxy: Doctor REFUZAT la GetFinancialSummary", false);
            } catch (UnauthorizedAccessException) {
                Assert("ProtectionProxy: Doctor REFUZAT la GetFinancialSummary", true);
            }

            // T8: Administrator poate GetFinancialSummary
            try {
                proxyAdmin.GetFinancialSummary();
                Assert("ProtectionProxy: Administrator poate GetFinancialSummary", true);
            } catch (UnauthorizedAccessException) {
                Assert("ProtectionProxy: Administrator poate GetFinancialSummary", false);
            }

            // ── Logging Proxy ──────────────────────────────────────────
            var logProxy = new LoggingProxy(real, "U2_Doctor");

            // T9: LoggingProxy înregistrează operațiile
            int before = logProxy.LogCount;
            logProxy.GetBasicInfo();
            logProxy.GetMedicalHistory();
            Assert("LoggingProxy: 2 operații jurnalizate",
                logProxy.LogCount == before + 2);

            // T10: Log conține user-ul și operația
            var log = logProxy.GetAuditLog();
            Assert("LoggingProxy: ultima intrare conține UserId",
                log.Last().UserId == "U2_Doctor");
            Assert("LoggingProxy: intrările conțin operații corecte",
                log.Any(e => e.Operation == "GetBasicInfo") &&
                log.Any(e => e.Operation == "GetMedicalHistory"));

            // T11: Chain – Logging + Protection împreună
            var chainProxy = new LoggingProxy(
                new ProtectionProxy(real, receptionist), "U1_Receptionist");

            // Recepționist accesează GetBasicInfo (permis) – logat
            chainProxy.GetBasicInfo();
            Assert("Chain Proxy: GetBasicInfo logat cu succes", chainProxy.LogCount >= 1);

            // Recepționist încearcă GetMedicalHistory (interzis) – logat ca FAIL
            try { chainProxy.GetMedicalHistory(); } catch { }
            var failLog = chainProxy.GetAuditLog()
                .FirstOrDefault(e => e.Operation == "GetMedicalHistory");
            Assert("Chain Proxy: acces refuzat jurnalizat ca FAIL",
                failLog != null && !failLog.Success);
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
