// ═══════════════════════════════════════════════════════════════════════
//  ABSTRACT FACTORY PATTERN
//  Domeniu: Pachete de tratament pentru clinica stomatologică
//
//  Scenariu: Clinica oferă 3 tipuri de pachete (Basic, Premium, Pediatric).
//  Fiecare pachet definește o FAMILIE de obiecte corelate:
//    • ITreatmentPlan   – planul de tratament
//    • IInstrumentSet   – setul de instrumente folosit
//    • IBillingStrategy – strategia de facturare
//
//  Abstract Factory garantează că obiectele dintr-o familie
//  sunt COMPATIBILE între ele (nu poți amesteca Basic cu Premium).
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab2.AbstractFactory
{
    // ───────────────────────────────────────────────────────────────────
    // 1. PRODUSE ABSTRACTE – interfețele familiei
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Plan de tratament – ce proceduri include pachetul.</summary>
    public interface ITreatmentPlan
    {
        string PlanName       { get; }
        int    MaxVisits      { get; }
        List<string> IncludedProcedures { get; }
        string Describe();
    }

    /// <summary>Set de instrumente folosit în cadrul pachetului.</summary>
    public interface IInstrumentSet
    {
        string SetName { get; }
        List<string> Instruments { get; }
        bool IncludesDigitalXRay { get; }
        string Describe();
    }

    /// <summary>Strategia de facturare a pachetului.</summary>
    public interface IBillingStrategy
    {
        string StrategyName { get; }
        decimal BasePrice   { get; }
        decimal CalculateFinalPrice(int numberOfVisits, bool hasInsurance);
        string  GetInvoiceDescription();
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. PRODUSE CONCRETE – familia BASIC
    // ───────────────────────────────────────────────────────────────────

    public class BasicTreatmentPlan : ITreatmentPlan
    {
        public string PlanName  => "Basic Plan";
        public int    MaxVisits => 3;
        public List<string> IncludedProcedures =>
        [
            "Consultație generală",
            "Detartraj simplu",
            "Obturație 1 dinte"
        ];
        public string Describe() =>
            $"[{PlanName}] Max {MaxVisits} vizite | " +
            $"Proceduri: {string.Join(", ", IncludedProcedures)}";
    }

    public class BasicInstrumentSet : IInstrumentSet
    {
        public string SetName => "Basic Instrument Set";
        public List<string> Instruments =>
        [
            "Oglindă dentară", "Sondă", "Pensă", "Excavator", "Turbină standard"
        ];
        public bool IncludesDigitalXRay => false;
        public string Describe() =>
            $"[{SetName}] Instrumente: {string.Join(", ", Instruments)} | " +
            $"Radiografie digitală: {(IncludesDigitalXRay ? "Da" : "Nu")}";
    }

    public class BasicBillingStrategy : IBillingStrategy
    {
        public string  StrategyName => "Tarif Standard";
        public decimal BasePrice    => 500m;

        public decimal CalculateFinalPrice(int numberOfVisits, bool hasInsurance)
        {
            decimal total = BasePrice * numberOfVisits;
            if (hasInsurance) total *= 0.85m; // 15% reducere asigurare
            return Math.Round(total, 2);
        }

        public string GetInvoiceDescription() =>
            $"{StrategyName} | Preț/vizită: {BasePrice:C} | " +
            $"Reducere asigurare: 15%";
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. PRODUSE CONCRETE – familia PREMIUM
    // ───────────────────────────────────────────────────────────────────

    public class PremiumTreatmentPlan : ITreatmentPlan
    {
        public string PlanName  => "Premium Plan";
        public int    MaxVisits => 10;
        public List<string> IncludedProcedures =>
        [
            "Consultație specializată",
            "Detartraj ultrasonic + airflow",
            "Obturații multiple",
            "Albire profesională",
            "Radiografie panoramică",
            "Plan de tratament ortodontic"
        ];
        public string Describe() =>
            $"[{PlanName}] Max {MaxVisits} vizite | " +
            $"Proceduri: {string.Join(", ", IncludedProcedures)}";
    }

    public class PremiumInstrumentSet : IInstrumentSet
    {
        public string SetName => "Premium Instrument Set";
        public List<string> Instruments =>
        [
            "Aparat ultrasonic", "Turbină de ultimă generație", "Microscop dentar",
            "Scanner intraoral 3D", "Laser dentar", "Kit albire profesional"
        ];
        public bool IncludesDigitalXRay => true;
        public string Describe() =>
            $"[{SetName}] Instrumente: {string.Join(", ", Instruments)} | " +
            $"Radiografie digitală: {(IncludesDigitalXRay ? "Da" : "Nu")}";
    }

    public class PremiumBillingStrategy : IBillingStrategy
    {
        public string  StrategyName => "Tarif Premium cu Abonament";
        public decimal BasePrice    => 1200m;

        public decimal CalculateFinalPrice(int numberOfVisits, bool hasInsurance)
        {
            // Primele 3 vizite la preț întreg, restul cu 20% reducere
            decimal total = numberOfVisits <= 3
                ? BasePrice * numberOfVisits
                : BasePrice * 3 + BasePrice * 0.80m * (numberOfVisits - 3);

            if (hasInsurance) total *= 0.90m; // 10% reducere asigurare
            return Math.Round(total, 2);
        }

        public string GetInvoiceDescription() =>
            $"{StrategyName} | Preț/vizită: {BasePrice:C} | " +
            $"Vizite 4+: -20% | Reducere asigurare: 10%";
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. PRODUSE CONCRETE – familia PEDIATRIC
    // ───────────────────────────────────────────────────────────────────

    public class PediatricTreatmentPlan : ITreatmentPlan
    {
        public string PlanName  => "Pediatric Plan";
        public int    MaxVisits => 6;
        public List<string> IncludedProcedures =>
        [
            "Consultație pediatrică",
            "Sigilare molari",
            "Aplicare fluor",
            "Extracție dinte de lapte",
            "Obturație colorată"
        ];
        public string Describe() =>
            $"[{PlanName}] Max {MaxVisits} vizite | " +
            $"Proceduri: {string.Join(", ", IncludedProcedures)}";
    }

    public class PediatricInstrumentSet : IInstrumentSet
    {
        public string SetName => "Pediatric Instrument Set";
        public List<string> Instruments =>
        [
            "Instrumentar pediatric mic", "Turbină silenţioasă",
            "Materiale colorate", "Kit aplicare fluor", "Mănuși aromate"
        ];
        public bool IncludesDigitalXRay => false;
        public string Describe() =>
            $"[{SetName}] Instrumente: {string.Join(", ", Instruments)} | " +
            $"Radiografie digitală: {(IncludesDigitalXRay ? "Da" : "Nu")}";
    }

    public class PediatricBillingStrategy : IBillingStrategy
    {
        public string  StrategyName => "Tarif Pediatric";
        public decimal BasePrice    => 300m;

        public decimal CalculateFinalPrice(int numberOfVisits, bool hasInsurance)
        {
            decimal total = BasePrice * numberOfVisits;
            if (hasInsurance) total *= 0.80m; // 20% reducere pentru copii + asigurare
            return Math.Round(total, 2);
        }

        public string GetInvoiceDescription() =>
            $"{StrategyName} | Preț/vizită: {BasePrice:C} | " +
            $"Reducere asigurare copii: 20%";
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. ABSTRACT FACTORY – interfața fabricii
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fabrica abstractă. Definește metodele de creare
    /// pentru TOATĂ familia de produse.
    /// Clientul depinde doar de această interfață (DIP).
    /// </summary>
    public interface ITreatmentPackageFactory
    {
        string PackageName { get; }
        ITreatmentPlan    CreateTreatmentPlan();
        IInstrumentSet    CreateInstrumentSet();
        IBillingStrategy  CreateBillingStrategy();
    }

    // ───────────────────────────────────────────────────────────────────
    // 6. FABRICI CONCRETE – implementează Abstract Factory
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Fabrica pentru pachetul Basic.</summary>
    public class BasicPackageFactory : ITreatmentPackageFactory
    {
        public string PackageName => "Basic Package";
        public ITreatmentPlan   CreateTreatmentPlan()   => new BasicTreatmentPlan();
        public IInstrumentSet   CreateInstrumentSet()   => new BasicInstrumentSet();
        public IBillingStrategy CreateBillingStrategy() => new BasicBillingStrategy();
    }

    /// <summary>Fabrica pentru pachetul Premium.</summary>
    public class PremiumPackageFactory : ITreatmentPackageFactory
    {
        public string PackageName => "Premium Package";
        public ITreatmentPlan   CreateTreatmentPlan()   => new PremiumTreatmentPlan();
        public IInstrumentSet   CreateInstrumentSet()   => new PremiumInstrumentSet();
        public IBillingStrategy CreateBillingStrategy() => new PremiumBillingStrategy();
    }

    /// <summary>Fabrica pentru pachetul Pediatric.</summary>
    public class PediatricPackageFactory : ITreatmentPackageFactory
    {
        public string PackageName => "Pediatric Package";
        public ITreatmentPlan   CreateTreatmentPlan()   => new PediatricTreatmentPlan();
        public IInstrumentSet   CreateInstrumentSet()   => new PediatricInstrumentSet();
        public IBillingStrategy CreateBillingStrategy() => new PediatricBillingStrategy();
    }

    // ───────────────────────────────────────────────────────────────────
    // 7. CLIENT – folosește Abstract Factory fără să cunoască concretele
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serviciul de consultanță pentru pacienți.
    /// Primește o fabrică și construiește un pachet complet.
    /// Nu știe nimic despre Basic/Premium/Pediatric concret.
    /// </summary>
    public class TreatmentPackageConsultant
    {
        private readonly ITreatmentPackageFactory _factory;

        // DIP: depinde de abstractizare, nu de fabrici concrete
        public TreatmentPackageConsultant(ITreatmentPackageFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Generează oferta completă pentru un pacient.
        /// </summary>
        public void PresentPackageToPatient(
            string patientName,
            int    plannedVisits,
            bool   hasInsurance)
        {
            // Creează familia de obiecte – toate compatibile între ele
            ITreatmentPlan   plan      = _factory.CreateTreatmentPlan();
            IInstrumentSet   instruments = _factory.CreateInstrumentSet();
            IBillingStrategy billing   = _factory.CreateBillingStrategy();

            decimal finalPrice = billing.CalculateFinalPrice(plannedVisits, hasInsurance);

            Console.WriteLine($"\n{'═',1}{'═',54}");
            Console.WriteLine($"  OFERTĂ: {_factory.PackageName} pentru {patientName}");
            Console.WriteLine($"{'═',1}{'═',54}");
            Console.WriteLine($"  Plan tratament : {plan.Describe()}");
            Console.WriteLine($"  Instrumente    : {instruments.Describe()}");
            Console.WriteLine($"  Facturare       : {billing.GetInvoiceDescription()}");
            Console.WriteLine($"  ────────────────────────────────────────────────────");
            Console.WriteLine($"  Vizite planificate : {plannedVisits}");
            Console.WriteLine($"  Asigurare          : {(hasInsurance ? "Da" : "Nu")}");
            Console.WriteLine($"  TOTAL DE PLATĂ     : {finalPrice:C}");
            Console.WriteLine($"{'═',1}{'═',54}\n");
        }
    }
}
