// ═══════════════════════════════════════════════════════════════════════
//  VISITOR PATTERN
//  Domeniu: Operații multiple pe structura de servicii a clinicii
//
//  Scenariu: Clinica are o structură de servicii (consultații,
//  tratamente, intervenții chirurgicale, estetică). Trebuie să
//  efectuăm diverse operații pe această structură:
//   • CostCalculatorVisitor  – calculează costul total cu reduceri
//   • TaxVisitor             – calculează TVA per categorie
//   • ExportCsvVisitor       – exportă în format CSV
//   • ExportJsonVisitor      – exportă în format JSON
//   • InsuranceCheckVisitor  – verifică ce e acoperit de asigurare
//
//  Visitor adaugă aceste operații fără a modifica clasele de servicii.
//  Adăugând un nou Visitor → nouă operație fără nicio modificare
//  la clasele existente (Open/Closed Principle).
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab7.Visitor
{
    // ───────────────────────────────────────────────────────────────────
    // 1. INTERFAȚA VISITOR
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața Visitor cu o metodă Visit pentru fiecare tip de element.
    /// Double dispatch: elementul apelează Visit(this), visitor-ul
    /// primește tipul concret fără cast.
    /// </summary>
    public interface IDentalServiceVisitor
    {
        void Visit(ConsultationService service);
        void Visit(TreatmentService    service);
        void Visit(SurgicalService     service);
        void Visit(AestheticService    service);
        void Visit(ServicePackage      package);
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. INTERFAȚA ELEMENT – orice serviciu acceptă vizitatori
    // ───────────────────────────────────────────────────────────────────

    public interface IDentalServiceElement
    {
        string Name     { get; }
        decimal Price   { get; }
        void Accept(IDentalServiceVisitor visitor);
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. ELEMENTE CONCRETE – serviciile clinicii
    // ───────────────────────────────────────────────────────────────────

    public class ConsultationService : IDentalServiceElement
    {
        public string  Name          { get; init; } = string.Empty;
        public decimal Price         { get; init; }
        public string  Code          { get; init; } = string.Empty;
        public bool    IsEmergency   { get; init; }
        public int     DurationMins  { get; init; }

        public void Accept(IDentalServiceVisitor v) => v.Visit(this);
    }

    public class TreatmentService : IDentalServiceElement
    {
        public string  Name              { get; init; } = string.Empty;
        public decimal Price             { get; init; }
        public string  Code              { get; init; } = string.Empty;
        public string  IcdCode           { get; init; } = string.Empty;
        public bool    RequiresAnesthesia { get; init; }
        public int     DurationMins      { get; init; }
        public bool    InsuranceCoverable { get; init; } = true;

        public void Accept(IDentalServiceVisitor v) => v.Visit(this);
    }

    public class SurgicalService : IDentalServiceElement
    {
        public string  Name              { get; init; } = string.Empty;
        public decimal Price             { get; init; }
        public string  Code              { get; init; } = string.Empty;
        public int     RiskLevel         { get; init; }   // 1-5
        public bool    RequiresHospital  { get; init; }
        public string  AnesthesiaType    { get; init; } = string.Empty;
        public int     DurationMins      { get; init; }

        public void Accept(IDentalServiceVisitor v) => v.Visit(this);
    }

    public class AestheticService : IDentalServiceElement
    {
        public string  Name             { get; init; } = string.Empty;
        public decimal Price            { get; init; }
        public string  Code             { get; init; } = string.Empty;
        public int     SessionsRequired { get; init; } = 1;
        public bool    IsInsuranceCovered { get; init; } = false;
        public int     DurationMins     { get; init; }

        public void Accept(IDentalServiceVisitor v) => v.Visit(this);
    }

    /// <summary>Pachet compus – conține mai multe servicii.</summary>
    public class ServicePackage : IDentalServiceElement
    {
        public string  Name             { get; init; } = string.Empty;
        public decimal DiscountRate     { get; init; }
        public List<IDentalServiceElement> Services { get; init; } = new();

        public decimal Price => Math.Round(
            Services.Sum(s => s.Price) * (1 - DiscountRate), 2);

        public void Accept(IDentalServiceVisitor v)
        {
            v.Visit(this);
            foreach (var s in Services)
                s.Accept(v);   // traversare recursivă
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. VIZITATORI CONCREȚI
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Visitor 1: Calculează costul total aplicând reduceri specifice.
    /// Consultații: -10% | Tratamente cu asigurare: -30% | Estetică: +10% tax
    /// </summary>
    public class CostCalculatorVisitor : IDentalServiceVisitor
    {
        public decimal TotalCost    { get; private set; }
        public decimal TotalSavings { get; private set; }
        private readonly List<string> _breakdown = new();
        public IReadOnlyList<string> Breakdown => _breakdown.AsReadOnly();

        public void Visit(ConsultationService s)
        {
            decimal disc = Math.Round(s.Price * 0.10m, 2);
            decimal net  = s.Price - disc;
            TotalCost    += net;
            TotalSavings += disc;
            _breakdown.Add($"  Consultație '{s.Name}': {s.Price:C} -10% = {net:C}");
        }

        public void Visit(TreatmentService s)
        {
            decimal disc = s.InsuranceCoverable ? Math.Round(s.Price * 0.30m, 2) : 0m;
            decimal net  = s.Price - disc;
            TotalCost    += net;
            TotalSavings += disc;
            _breakdown.Add($"  Tratament '{s.Name}': {s.Price:C}" +
                (disc > 0 ? $" -30% asig. = {net:C}" : $" = {net:C}"));
        }

        public void Visit(SurgicalService s)
        {
            decimal surcharge = s.RiskLevel >= 4 ? Math.Round(s.Price * 0.15m, 2) : 0m;
            decimal net       = s.Price + surcharge;
            TotalCost        += net;
            _breakdown.Add($"  Chirurgie '{s.Name}': {s.Price:C}" +
                (surcharge > 0 ? $" +15% risc = {net:C}" : $" = {net:C}"));
        }

        public void Visit(AestheticService s)
        {
            decimal total = s.Price * s.SessionsRequired;
            TotalCost    += total;
            _breakdown.Add($"  Estetică '{s.Name}': {s.Price:C} × {s.SessionsRequired} = {total:C}");
        }

        public void Visit(ServicePackage pkg)
        {
            _breakdown.Add($"\n  📦 Pachet '{pkg.Name}' (reducere {pkg.DiscountRate*100:F0}%):");
        }

        public void PrintReport()
        {
            Console.WriteLine("  ── Cost Calculator ─────────────────────────────");
            foreach (var l in _breakdown) Console.WriteLine(l);
            Console.WriteLine($"  Economii: {TotalSavings:C}");
            Console.WriteLine($"  TOTAL: {TotalCost:C}");
        }
    }

    /// <summary>Visitor 2: Calculează TVA per categorie de serviciu.</summary>
    public class TaxVisitor : IDentalServiceVisitor
    {
        private decimal _consultationTax;
        private decimal _treatmentTax;
        private decimal _surgicalTax;
        private decimal _aestheticTax;

        private const decimal ConsultationVat = 0.08m;
        private const decimal TreatmentVat    = 0.08m;
        private const decimal SurgicalVat     = 0.08m;
        private const decimal AestheticVat    = 0.20m;   // estetică: TVA normal

        public void Visit(ConsultationService s)
            => _consultationTax += Math.Round(s.Price * ConsultationVat, 2);

        public void Visit(TreatmentService s)
            => _treatmentTax += Math.Round(s.Price * TreatmentVat, 2);

        public void Visit(SurgicalService s)
            => _surgicalTax += Math.Round(s.Price * SurgicalVat, 2);

        public void Visit(AestheticService s)
            => _aestheticTax += Math.Round(s.Price * s.SessionsRequired * AestheticVat, 2);

        public void Visit(ServicePackage pkg)
            => Console.WriteLine($"  [TaxVisitor] Procesare pachet: {pkg.Name}");

        public decimal TotalTax =>
            _consultationTax + _treatmentTax + _surgicalTax + _aestheticTax;

        public void PrintReport()
        {
            Console.WriteLine("  ── Tax Visitor ─────────────────────────────────");
            Console.WriteLine($"  Consultații (TVA 8%): {_consultationTax:C}");
            Console.WriteLine($"  Tratamente  (TVA 8%): {_treatmentTax:C}");
            Console.WriteLine($"  Chirurgie   (TVA 8%): {_surgicalTax:C}");
            Console.WriteLine($"  Estetică   (TVA 20%): {_aestheticTax:C}");
            Console.WriteLine($"  TOTAL TVA           : {TotalTax:C}");
        }
    }

    /// <summary>Visitor 3: Export CSV.</summary>
    public class ExportCsvVisitor : IDentalServiceVisitor
    {
        private readonly System.Text.StringBuilder _csv = new();
        public string Result => _csv.ToString();

        public ExportCsvVisitor()
            => _csv.AppendLine("Categorie,Cod,Nume,Pret,Detalii");

        public void Visit(ConsultationService s)
            => _csv.AppendLine($"Consultatie,{s.Code},{Esc(s.Name)},{s.Price}," +
                $"{s.DurationMins}min{(s.IsEmergency?";urgenta":"")}");

        public void Visit(TreatmentService s)
            => _csv.AppendLine($"Tratament,{s.Code},{Esc(s.Name)},{s.Price}," +
                $"{s.IcdCode};{(s.RequiresAnesthesia?"anestezie":"")}");

        public void Visit(SurgicalService s)
            => _csv.AppendLine($"Chirurgie,{s.Code},{Esc(s.Name)},{s.Price}," +
                $"risc{s.RiskLevel};{s.AnesthesiaType}");

        public void Visit(AestheticService s)
            => _csv.AppendLine($"Estetica,{s.Code},{Esc(s.Name)},{s.Price}," +
                $"{s.SessionsRequired}sedinte");

        public void Visit(ServicePackage pkg)
            => _csv.AppendLine($"Pachet,,{Esc(pkg.Name)},{pkg.Price}," +
                $"reducere{pkg.DiscountRate*100:F0}%");

        private static string Esc(string s)
            => s.Contains(',') ? $"\"{s}\"" : s;
    }

    /// <summary>Visitor 4: Export JSON.</summary>
    public class ExportJsonVisitor : IDentalServiceVisitor
    {
        private readonly List<string> _items = new();
        public string Result =>
            "[\n" + string.Join(",\n", _items) + "\n]";

        public void Visit(ConsultationService s) => Add("consultatie", s.Code, s.Name, s.Price,
            $"\"duration\":{s.DurationMins},\"emergency\":{s.IsEmergency.ToString().ToLower()}");

        public void Visit(TreatmentService s) => Add("tratament", s.Code, s.Name, s.Price,
            $"\"icd\":\"{s.IcdCode}\",\"anesthesia\":{s.RequiresAnesthesia.ToString().ToLower()}");

        public void Visit(SurgicalService s) => Add("chirurgie", s.Code, s.Name, s.Price,
            $"\"riskLevel\":{s.RiskLevel},\"anesthesiaType\":\"{s.AnesthesiaType}\"");

        public void Visit(AestheticService s) => Add("estetica", s.Code, s.Name, s.Price,
            $"\"sessions\":{s.SessionsRequired},\"insured\":{s.IsInsuranceCovered.ToString().ToLower()}");

        public void Visit(ServicePackage pkg) => Add("pachet", "PKG", pkg.Name, pkg.Price,
            $"\"discount\":{pkg.DiscountRate},\"items\":{pkg.Services.Count}");

        private void Add(string cat, string code, string name, decimal price, string extra)
            => _items.Add(
                $"  {{\"category\":\"{cat}\",\"code\":\"{code}\"," +
                $"\"name\":\"{name}\",\"price\":{price},{extra}}}");
    }

    /// <summary>Visitor 5: Verifică ce servicii sunt acoperite de asigurare.</summary>
    public class InsuranceCheckVisitor : IDentalServiceVisitor
    {
        public List<string> CoveredServices   { get; } = new();
        public List<string> UncoveredServices { get; } = new();
        public decimal TotalCovered   { get; private set; }
        public decimal TotalUncovered { get; private set; }

        public void Visit(ConsultationService s)
        {
            // Consultațiile sunt acoperite de asigurare
            CoveredServices.Add($"Consultație: {s.Name} – {s.Price:C}");
            TotalCovered += s.Price;
        }

        public void Visit(TreatmentService s)
        {
            if (s.InsuranceCoverable)
            { CoveredServices.Add($"Tratament: {s.Name} – {s.Price:C}"); TotalCovered += s.Price; }
            else
            { UncoveredServices.Add($"Tratament: {s.Name} – {s.Price:C}"); TotalUncovered += s.Price; }
        }

        public void Visit(SurgicalService s)
        {
            // Chirurgie de urgență → acoperită
            if (s.RiskLevel >= 3)
            { CoveredServices.Add($"Chirurgie: {s.Name} – {s.Price:C}"); TotalCovered += s.Price; }
            else
            { UncoveredServices.Add($"Chirurgie: {s.Name} – {s.Price:C}"); TotalUncovered += s.Price; }
        }

        public void Visit(AestheticService s)
        {
            // Serviciile estetice nu sunt acoperite
            UncoveredServices.Add($"Estetică: {s.Name} – {s.Price:C}");
            TotalUncovered += s.Price;
        }

        public void Visit(ServicePackage pkg) { }

        public void PrintReport()
        {
            Console.WriteLine("  ── Insurance Check ─────────────────────────────");
            Console.WriteLine($"  Acoperite ({CoveredServices.Count}):");
            CoveredServices.ForEach(s => Console.WriteLine($"    ✅ {s}"));
            Console.WriteLine($"  Neacoperite ({UncoveredServices.Count}):");
            UncoveredServices.ForEach(s => Console.WriteLine($"    ❌ {s}"));
            Console.WriteLine($"  Total acoperit  : {TotalCovered:C}");
            Console.WriteLine($"  Total neacoperit: {TotalUncovered:C}");
        }
    }
}
