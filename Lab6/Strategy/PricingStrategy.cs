using System;
using DentalClinic.Models;

namespace DentalClinic.Strategy
{
    // ════════════════════════════════════════════════════════════════════════════
    //  STRATEGY – Calcul Cost Tratament
    //
    //  Problemă: prețul unui tratament variază în funcție de tipul pacientului
    //  (standard, VIP, asigurat) și de promoții. Fără Strategy, am avea un
    //  if/else uriaș în logica de programare, greu de extins.
    //
    //  Soluție: interfață IPricingStrategy + implementări concrete interschimbabile
    //  la runtime, fără a modifica clasa Appointment sau logica de rezervare.
    // ════════════════════════════════════════════════════════════════════════════

    // ─── Interfața comună ──────────────────────────────────────────────────────

    public interface IPricingStrategy
    {
        string StrategyName { get; }

        /// <summary>Calculează costul final pentru un pacient și un preț de bază.</summary>
        double CalculatePrice(double basePrice, Patient patient);

        /// <summary>Returnează un sumar text al regulilor aplicate.</summary>
        string GetDescription(double basePrice, Patient patient);
    }

    // ─── Standard Pricing ─────────────────────────────────────────────────────

    /// <summary>Preț integral fără nicio reducere sau majorare.</summary>
    public class StandardPricingStrategy : IPricingStrategy
    {
        public string StrategyName => "Standard";

        public double CalculatePrice(double basePrice, Patient patient)
            => Math.Round(basePrice, 2);

        public string GetDescription(double basePrice, Patient patient)
            => $"  Strategie        : Standard (fără reducere)\n" +
               $"  Preț de bază     : ${basePrice:F2}\n" +
               $"  TOTAL DE PLATĂ   : ${CalculatePrice(basePrice, patient):F2}";
    }

    // ─── VIP Pricing ──────────────────────────────────────────────────────────

    /// <summary>
    /// Pacienți VIP: reducere fixă de 20% + servicii prioritare incluse.
    /// Dacă prețul de bază depășește $500, se aplică o reducere suplimentară de 5%.
    /// </summary>
    public class VIPPricingStrategy : IPricingStrategy
    {
        private const double BaseDiscount        = 0.20;
        private const double HighValueThreshold  = 500.0;
        private const double ExtraDiscount       = 0.05;

        public string StrategyName => "VIP";

        public double CalculatePrice(double basePrice, Patient patient)
        {
            double discount = BaseDiscount;
            if (basePrice > HighValueThreshold)
                discount += ExtraDiscount;

            return Math.Round(basePrice * (1.0 - discount), 2);
        }

        public string GetDescription(double basePrice, Patient patient)
        {
            double discount = BaseDiscount;
            string extraNote = "";
            if (basePrice > HighValueThreshold)
            {
                discount += ExtraDiscount;
                extraNote = $"\n  Reducere extra   : -{ExtraDiscount * 100:F0}% (serviciu >$500)";
            }

            return $"  Strategie        : VIP\n" +
                   $"  Preț de bază     : ${basePrice:F2}\n" +
                   $"  Reducere VIP     : -{BaseDiscount * 100:F0}%{extraNote}\n" +
                   $"  Reducere totală  : -{discount * 100:F0}%\n" +
                   $"  TOTAL DE PLATĂ   : ${CalculatePrice(basePrice, patient):F2}";
        }
    }

    // ─── Insurance Pricing ────────────────────────────────────────────────────

    /// <summary>
    /// Pacienți asigurați: asigurătorul acoperă un procent (din Patient.InsuranceCoverage),
    /// restul este suportat de pacient. Se aplică TVA de 20% la suma pacientului.
    /// </summary>
    public class InsurancePricingStrategy : IPricingStrategy
    {
        private const double VAT = 0.20;

        public string StrategyName => "Asigurare";

        public double CalculatePrice(double basePrice, Patient patient)
        {
            double coverage    = patient.InsuranceCoverage;
            double patientPart = basePrice * (1.0 - coverage);
            double withVat     = patientPart * (1.0 + VAT);
            return Math.Round(withVat, 2);
        }

        public string GetDescription(double basePrice, Patient patient)
        {
            double coverage    = patient.InsuranceCoverage;
            double insurerPart = basePrice * coverage;
            double patientPart = basePrice * (1.0 - coverage);
            double withVat     = patientPart * (1.0 + VAT);

            return $"  Strategie        : Asigurare\n" +
                   $"  Preț de bază     : ${basePrice:F2}\n" +
                   $"  Polița           : {patient.InsurancePolicyNumber}\n" +
                   $"  Acoperire asig.  : {coverage * 100:F0}% → ${insurerPart:F2}\n" +
                   $"  Rest pacient     : ${patientPart:F2}\n" +
                   $"  TVA (20%)        : ${patientPart * VAT:F2}\n" +
                   $"  TOTAL DE PLATĂ   : ${Math.Round(withVat, 2):F2}";
        }
    }

    // ─── Promotional Pricing ──────────────────────────────────────────────────

    /// <summary>
    /// Prețuri promoționale: reducere procentuală configurabilă + limită maximă
    /// de reducere în valoare absolută (plafon). Util pentru campanii sezoniere.
    /// </summary>
    public class PromotionalPricingStrategy : IPricingStrategy
    {
        private readonly double _discountPercent;
        private readonly double _maxDiscountAmount;
        private readonly string _promoCode;

        public string StrategyName => $"Promoție ({_promoCode})";

        public PromotionalPricingStrategy(string promoCode,
                                          double discountPercent,
                                          double maxDiscountAmount = double.MaxValue)
        {
            _promoCode         = promoCode;
            _discountPercent   = discountPercent;
            _maxDiscountAmount = maxDiscountAmount;
        }

        public double CalculatePrice(double basePrice, Patient patient)
        {
            double discount = Math.Min(basePrice * _discountPercent, _maxDiscountAmount);
            return Math.Round(basePrice - discount, 2);
        }

        public string GetDescription(double basePrice, Patient patient)
        {
            double discount    = Math.Min(basePrice * _discountPercent, _maxDiscountAmount);
            double finalPrice  = basePrice - discount;
            bool   capApplied  = basePrice * _discountPercent > _maxDiscountAmount;

            return $"  Strategie        : Promo '{_promoCode}'\n" +
                   $"  Preț de bază     : ${basePrice:F2}\n" +
                   $"  Reducere         : -{_discountPercent * 100:F0}% = ${basePrice * _discountPercent:F2}" +
                   (capApplied ? $" (plafonat la ${_maxDiscountAmount:F2})" : "") + "\n" +
                   $"  Reducere aplicată: -${discount:F2}\n" +
                   $"  TOTAL DE PLATĂ   : ${Math.Round(finalPrice, 2):F2}";
        }
    }

    // ─── Context: AppointmentPricingContext ──────────────────────────────────

    /// <summary>
    /// Context care utilizează o strategie injectată și permite schimbarea
    /// dinamică a acesteia la runtime fără a afecta restul sistemului.
    /// </summary>
    public class AppointmentPricingContext
    {
        private IPricingStrategy _strategy;

        public AppointmentPricingContext(IPricingStrategy strategy)
        {
            _strategy = strategy;
        }

        /// <summary>Schimbă strategia la runtime – fără a recompila nimic.</summary>
        public void SetStrategy(IPricingStrategy strategy)
        {
            Console.WriteLine($"  [Strategy] Strategie schimbată: {_strategy.StrategyName} → {strategy.StrategyName}");
            _strategy = strategy;
        }

        public IPricingStrategy CurrentStrategy => _strategy;

        public double CalculateFinalPrice(Appointment appointment)
            => _strategy.CalculatePrice(appointment.BasePrice, appointment.Patient);

        public void PrintPricingBreakdown(Appointment appointment)
        {
            Console.WriteLine($"  Pacient          : {appointment.Patient.Name}");
            Console.WriteLine($"  Serviciu         : {appointment.Service}");
            Console.WriteLine(_strategy.GetDescription(appointment.BasePrice, appointment.Patient));
        }
    }

    // ─── Factory helper ───────────────────────────────────────────────────────

    public static class PricingStrategyFactory
    {
        /// <summary>Selectează automat strategia potrivită pe baza categoriei pacientului.</summary>
        public static IPricingStrategy ForPatient(Patient patient)
        {
            return patient.Category switch
            {
                PatientCategory.VIP      => new VIPPricingStrategy(),
                PatientCategory.Insured  => new InsurancePricingStrategy(),
                _                        => new StandardPricingStrategy()
            };
        }
    }
}
