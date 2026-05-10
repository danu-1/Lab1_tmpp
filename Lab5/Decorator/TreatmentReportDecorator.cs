// ═══════════════════════════════════════════════════════════════════════
//  DECORATOR PATTERN
//  Domeniu: Rapoarte de tratament cu funcționalități adăugabile dinamic
//
//  Scenariu: Clinica generează rapoarte despre tratamentele pacienților.
//  Un raport de bază conține informațiile esențiale. Diverse părți
//  interesate cer funcționalități adiționale:
//    • Secretara: adaugă antet + subsol cu datele clinicii
//    • Contabilitatea: adaugă calculul TVA și detalii financiare
//    • Medicul: adaugă secțiunea de diagnostic și imagini RX
//    • Asigurarea: adaugă codurile de proceduri ICD-10
//    • Administrația: adaugă watermark "Confidențial"
//
//  Decorator adaugă aceste funcționalități DINAMIC, prin învelituri
//  succesive – fără a modifica clasa raport de bază și fără
//  explozie combinatorică de subclase.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab5.Decorator
{
    // ───────────────────────────────────────────────────────────────────
    // 1. COMPONENTA DE BAZĂ – interfața comună
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața componentei. Atât obiectul de bază cât și toți
    /// decoratorii implementează această interfață.
    /// </summary>
    public interface ITreatmentReport
    {
        string Generate();
        string Title    { get; }
        decimal GetCost();
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. COMPONENTA CONCRETĂ – raportul de bază
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Raportul minimal cu informațiile esențiale ale tratamentului.
    /// Acesta este obiectul pe care decoratorii îl vor "împacheta".
    /// </summary>
    public class BasicTreatmentReport : ITreatmentReport
    {
        private readonly string   _patientName;
        private readonly string   _doctorName;
        private readonly DateTime _treatmentDate;
        private readonly string   _diagnosis;
        private readonly string   _procedure;
        private readonly decimal  _cost;

        public BasicTreatmentReport(
            string patientName, string doctorName,
            DateTime treatmentDate, string diagnosis,
            string procedure, decimal cost)
        {
            _patientName   = patientName;
            _doctorName    = doctorName;
            _treatmentDate = treatmentDate;
            _diagnosis     = diagnosis;
            _procedure     = procedure;
            _cost          = cost;
        }

        public string  Title    => "Raport Tratament";
        public decimal GetCost() => _cost;

        public string Generate() =>
            $"Pacient  : {_patientName}\n"    +
            $"Medic    : {_doctorName}\n"     +
            $"Data     : {_treatmentDate:dd.MM.yyyy}\n" +
            $"Diagnostic: {_diagnosis}\n"    +
            $"Procedură : {_procedure}\n"    +
            $"Cost bază : {_cost:C}";
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. DECORATORUL ABSTRACT – baza tuturor decoratorilor
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Decoratorul abstract ține referința la componenta învelită
    /// și delegă apelurile de bază către ea.
    /// </summary>
    public abstract class TreatmentReportDecorator : ITreatmentReport
    {
        protected readonly ITreatmentReport _wrapped;

        protected TreatmentReportDecorator(ITreatmentReport report)
            => _wrapped = report;

        public virtual string  Title    => _wrapped.Title;
        public virtual decimal GetCost() => _wrapped.GetCost();

        // Fiecare decorator concret suprascrie Generate()
        // și apelează _wrapped.Generate() pentru a păstra conținutul existent
        public abstract string Generate();
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. DECORATORI CONCREȚI
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Decorator 1: Antet și subsol oficial al clinicii.
    /// Folosit de secretară pentru orice document oficial.
    /// </summary>
    public class ClinicHeaderDecorator : TreatmentReportDecorator
    {
        private readonly string _clinicName;
        private readonly string _address;
        private readonly string _licenseNo;

        public ClinicHeaderDecorator(ITreatmentReport report,
            string clinicName, string address, string licenseNo)
            : base(report)
        {
            _clinicName = clinicName;
            _address    = address;
            _licenseNo  = licenseNo;
        }

        public override string Title => $"[Antet Oficial] {_wrapped.Title}";

        public override string Generate()
        {
            var sep    = new string('─', 50);
            var header =
                $"{'═',50}\n"                                   +
                $"  {_clinicName}\n"                            +
                $"  {_address}\n"                               +
                $"  Licență: {_licenseNo}\n"                    +
                $"  Generat: {DateTime.Now:dd.MM.yyyy HH:mm}\n" +
                $"{sep}\n";

            var footer =
                $"\n{sep}\n"                                    +
                $"  Semnătura medicului: ___________________\n" +
                $"  Ștampila clinicii:   ___________________\n" +
                $"{'═',50}";

            return header + _wrapped.Generate() + footer;
        }
    }

    /// <summary>
    /// Decorator 2: Calcule TVA și detalii financiare.
    /// Folosit de departamentul contabilitate.
    /// </summary>
    public class FinancialDecorator : TreatmentReportDecorator
    {
        private readonly decimal _vatRate;
        private readonly string  _paymentMethod;

        public FinancialDecorator(ITreatmentReport report,
            decimal vatRate = 0.20m, string paymentMethod = "Card")
            : base(report)
        {
            _vatRate       = vatRate;
            _paymentMethod = paymentMethod;
        }

        public override string Title => $"[Financiar] {_wrapped.Title}";

        public override decimal GetCost()
        {
            decimal base_   = _wrapped.GetCost();
            return Math.Round(base_ * (1 + _vatRate), 2);
        }

        public override string Generate()
        {
            decimal baseCost = _wrapped.GetCost();
            decimal vatAmt   = Math.Round(baseCost * _vatRate, 2);
            decimal total    = baseCost + vatAmt;

            var financial =
                $"\n── DETALII FINANCIARE ──────────────────────────\n" +
                $"  Cost serviciu (fără TVA) : {baseCost:C}\n"          +
                $"  TVA ({_vatRate * 100:F0}%)            : {vatAmt:C}\n" +
                $"  TOTAL DE PLATĂ           : {total:C}\n"              +
                $"  Metodă plată             : {_paymentMethod}\n"       +
                $"  Nr. Fiscal               : {GenerateFiscalNumber()}";

            return _wrapped.Generate() + financial;
        }

        private static string GenerateFiscalNumber()
            => $"FC-{DateTime.Now:yyyyMMdd}-{new Random().Next(10000, 99999)}";
    }

    /// <summary>
    /// Decorator 3: Informații medicale detaliate și coduri ICD-10.
    /// Folosit pentru dosarul medical complet și dosarul de asigurare.
    /// </summary>
    public class MedicalDetailsDecorator : TreatmentReportDecorator
    {
        private readonly string _icdCode;
        private readonly string _anesthesiaType;
        private readonly List<string> _complications;
        private readonly string _followUp;

        public MedicalDetailsDecorator(ITreatmentReport report,
            string icdCode, string anesthesiaType,
            List<string>? complications = null, string followUp = "")
            : base(report)
        {
            _icdCode        = icdCode;
            _anesthesiaType = anesthesiaType;
            _complications  = complications ?? new List<string>();
            _followUp       = followUp;
        }

        public override string Title => $"[Detalii Medicale] {_wrapped.Title}";

        public override string Generate()
        {
            var compStr = _complications.Count > 0
                ? string.Join(", ", _complications)
                : "Niciuna";

            var medical =
                $"\n── DETALII MEDICALE ────────────────────────────\n"  +
                $"  Cod ICD-10       : {_icdCode}\n"                     +
                $"  Tip anestezie    : {_anesthesiaType}\n"               +
                $"  Complicații      : {compStr}\n"                       +
                $"  Follow-up        : {_followUp}\n"                     +
                $"  Capacitate lucru : Normală, fără restricții";

            return _wrapped.Generate() + medical;
        }
    }

    /// <summary>
    /// Decorator 4: Watermark "Confidențial" sau "Copie".
    /// Aplicat pe documentele sensibile.
    /// </summary>
    public class WatermarkDecorator : TreatmentReportDecorator
    {
        private readonly string _watermarkText;

        public WatermarkDecorator(ITreatmentReport report, string watermarkText = "CONFIDENȚIAL")
            : base(report) => _watermarkText = watermarkText;

        public override string Title => $"[{_watermarkText}] {_wrapped.Title}";

        public override string Generate()
        {
            string wm     = $"\n  *** {_watermarkText} – CIRCULAȚIE RESTRICȚIONATĂ ***\n";
            string border = new string('*', 52);
            return $"{border}{wm}{border}\n\n" + _wrapped.Generate()
                + $"\n\n{border}{wm}{border}";
        }
    }

    /// <summary>
    /// Decorator 5: Informații asigurare și coduri de decontare.
    /// Folosit pentru transmiterea la casa de asigurări.
    /// </summary>
    public class InsuranceDecorator : TreatmentReportDecorator
    {
        private readonly string  _insuranceProvider;
        private readonly string  _policyNumber;
        private readonly decimal _coveragePercent;

        public InsuranceDecorator(ITreatmentReport report,
            string insuranceProvider, string policyNumber, decimal coveragePercent)
            : base(report)
        {
            _insuranceProvider = insuranceProvider;
            _policyNumber      = policyNumber;
            _coveragePercent   = coveragePercent;
        }

        public override string Title => $"[Asigurare] {_wrapped.Title}";

        public override decimal GetCost()
        {
            // Costul din perspectiva pacientului = partea neacoperită de asigurare
            decimal base_ = _wrapped.GetCost();
            return Math.Round(base_ * (1 - _coveragePercent), 2);
        }

        public override string Generate()
        {
            decimal total    = _wrapped.GetCost();
            decimal covered  = Math.Round(total * _coveragePercent, 2);
            decimal patient  = total - covered;

            var insurance =
                $"\n── INFORMAȚII ASIGURARE ────────────────────────\n" +
                $"  Asigurător       : {_insuranceProvider}\n"           +
                $"  Polița nr.       : {_policyNumber}\n"                +
                $"  Acoperire        : {_coveragePercent * 100:F0}%\n"   +
                $"  Decontat de asig.: {covered:C}\n"                    +
                $"  Rest pacient     : {patient:C}";

            return _wrapped.Generate() + insurance;
        }
    }
}
