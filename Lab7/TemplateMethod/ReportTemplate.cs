// ═══════════════════════════════════════════════════════════════════════
//  TEMPLATE METHOD PATTERN
//  Domeniu: Generarea rapoartelor medicale
//
//  Scenariu: Clinica generează mai multe tipuri de rapoarte, toate
//  cu același flux general (colectare date → validare → formatare
//  header → corp specific → footer → export), dar cu conținut
//  specific fiecărui tip.
//
//  Template Method definește scheletul algoritmului în clasa de bază.
//  Subclasele suprascriu pașii specifici (metode abstracte/virtuale),
//  dar NU pot modifica ordinea pașilor (metoda template e sealed).
//
//  Rapoarte implementate:
//   • TreatmentSummaryReport   – sumar tratamente per pacient
//   • FinancialReport          – venituri per perioadă
//   • DoctorPerformanceReport  – performanța medicilor
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab7.TemplateMethod
{
    // ───────────────────────────────────────────────────────────────────
    // MODEL DE DATE
    // ───────────────────────────────────────────────────────────────────

    public record TreatmentEntry(
        string PatientName, string DoctorName, string Treatment,
        DateTime Date, decimal Cost, bool Completed);

    public record DoctorStats(
        string Name, int Appointments, int Completed,
        decimal TotalRevenue, double AvgDurationMins);

    // ───────────────────────────────────────────────────────────────────
    // 1. CLASA ABSTRACTĂ cu Template Method
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clasa de bază definește algoritmul de generare.
    /// GenerateReport() este metoda template — SEALED, nu poate fi
    /// suprascrisă. Subclasele personalizează pașii abstracti/virtuali.
    /// </summary>
    public abstract class MedicalReportGenerator
    {
        // ── TEMPLATE METHOD (sealed = ordinea pașilor e fixă) ──────────
        public string GenerateReport()
        {
            var sb = new System.Text.StringBuilder();

            // Pasul 1 – validare date (poate fi suprascrisa)
            ValidateData();

            // Pasul 2 – header comun tuturor rapoartelor
            sb.AppendLine(GenerateHeader());
            sb.AppendLine(new string('═', 56));

            // Pasul 3 – introducere specifică tipului de raport
            sb.AppendLine(GenerateIntroduction());
            sb.AppendLine(new string('─', 56));

            // Pasul 4 – corpul raportului (specific fiecărei subclase)
            sb.AppendLine(GenerateBody());
            sb.AppendLine(new string('─', 56));

            // Pasul 5 – secțiunea de sumar/statistici (opțională)
            string summary = GenerateSummary();
            if (!string.IsNullOrWhiteSpace(summary))
            {
                sb.AppendLine(summary);
                sb.AppendLine(new string('─', 56));
            }

            // Pasul 6 – hook: acțiune opțională post-generare
            OnReportGenerated();

            // Pasul 7 – footer comun
            sb.AppendLine(GenerateFooter());

            return sb.ToString();
        }

        // ── Pași ABSTRACȚI – obligatoriu de implementat în subclase ───
        protected abstract string ReportTitle      { get; }
        protected abstract string GenerateIntroduction();
        protected abstract string GenerateBody();

        // ── Pași VIRTUALI – comportament implicit, opțional override ──
        protected virtual string GenerateHeader() =>
            $"  RAPORT: {ReportTitle}\n" +
            $"  Clinică: DentaCare Clinic SRL\n" +
            $"  Generat: {DateTime.Now:dd.MM.yyyy HH:mm}\n" +
            $"  Autor: Sistem Automat";

        protected virtual string GenerateSummary() => string.Empty;

        protected virtual string GenerateFooter() =>
            $"  *** Raport generat automat – DentaCare Clinic ***\n" +
            $"  Semnătura: ____________________  Data: {DateTime.Today:dd.MM.yyyy}";

        // ── Hook – metodă opțională, subclasele pot suprascrie ────────
        protected virtual void ValidateData() { }
        protected virtual void OnReportGenerated()
            => Console.WriteLine($"  [Template] Raport '{ReportTitle}' generat cu succes.");
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. CLASE CONCRETE – personalizează pașii specifici
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Raport sumar tratamente per pacient.</summary>
    public class TreatmentSummaryReport : MedicalReportGenerator
    {
        private readonly string                _patientName;
        private readonly List<TreatmentEntry>  _entries;

        public TreatmentSummaryReport(
            string patientName, List<TreatmentEntry> entries)
        {
            _patientName = patientName;
            _entries     = entries;
        }

        protected override string ReportTitle => "Sumar Tratamente Pacient";

        protected override void ValidateData()
        {
            if (string.IsNullOrWhiteSpace(_patientName))
                throw new InvalidOperationException("Numele pacientului este obligatoriu.");
            if (_entries.Count == 0)
                throw new InvalidOperationException("Nu există tratamente de raportat.");
        }

        protected override string GenerateIntroduction() =>
            $"  Pacient: {_patientName}\n" +
            $"  Perioadă: {_entries.Min(e => e.Date):dd.MM.yyyy} – " +
            $"{_entries.Max(e => e.Date):dd.MM.yyyy}\n" +
            $"  Total tratamente: {_entries.Count}";

        protected override string GenerateBody()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  TRATAMENTE:");
            int i = 1;
            foreach (var e in _entries.OrderByDescending(e => e.Date))
            {
                sb.AppendLine($"  {i++,2}. [{e.Date:dd.MM.yyyy}] {e.Treatment,-30} " +
                              $"Dr.{e.DoctorName,-15} {e.Cost,8:C} " +
                              $"[{(e.Completed ? "✓" : "…")}]");
            }
            return sb.ToString();
        }

        protected override string GenerateSummary()
        {
            decimal total     = _entries.Sum(e => e.Cost);
            int     completed = _entries.Count(e => e.Completed);
            return $"  SUMAR FINANCIAR:\n" +
                   $"  Total plătit: {total:C} | " +
                   $"Finalizate: {completed}/{_entries.Count}";
        }
    }

    /// <summary>Raport financiar pe perioadă.</summary>
    public class FinancialReport : MedicalReportGenerator
    {
        private readonly DateTime              _from;
        private readonly DateTime              _to;
        private readonly List<TreatmentEntry>  _entries;
        private readonly decimal               _vatRate;

        public FinancialReport(DateTime from, DateTime to,
            List<TreatmentEntry> entries, decimal vatRate = 0.20m)
        {
            _from    = from;
            _to      = to;
            _entries = entries.Where(e => e.Date >= from && e.Date <= to).ToList();
            _vatRate = vatRate;
        }

        protected override string ReportTitle => "Raport Financiar";

        protected override void ValidateData()
        {
            if (_from > _to)
                throw new InvalidOperationException("Data de start > data de sfârșit.");
        }

        protected override string GenerateIntroduction() =>
            $"  Perioadă: {_from:dd.MM.yyyy} – {_to:dd.MM.yyyy}\n" +
            $"  Intrări: {_entries.Count} tratamente facturate";

        protected override string GenerateBody()
        {
            var byDoctor = _entries
                .GroupBy(e => e.DoctorName)
                .Select(g => new
                {
                    Doctor   = g.Key,
                    Count    = g.Count(),
                    Revenue  = g.Sum(e => e.Cost)
                });

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  VENITURI PE MEDIC:");
            foreach (var d in byDoctor.OrderByDescending(d => d.Revenue))
                sb.AppendLine($"  Dr.{d.Doctor,-20} {d.Count,3} tratamente   {d.Revenue,10:C}");

            return sb.ToString();
        }

        protected override string GenerateSummary()
        {
            decimal gross = _entries.Sum(e => e.Cost);
            decimal vat   = Math.Round(gross * _vatRate, 2);
            decimal net   = gross - vat;
            return $"  TOTAL BRUT : {gross:C}\n" +
                   $"  TVA ({_vatRate*100:F0}%) : {vat:C}\n" +
                   $"  TOTAL NET  : {net:C}";
        }

        // Hook suprascrisa: arhivăm raportul financiar
        protected override void OnReportGenerated()
        {
            base.OnReportGenerated();
            Console.WriteLine($"  [FinancialReport] Raport arhivat pentru audit.");
        }
    }

    /// <summary>Raport performanță medici.</summary>
    public class DoctorPerformanceReport : MedicalReportGenerator
    {
        private readonly List<DoctorStats> _stats;
        private readonly DateTime          _period;

        public DoctorPerformanceReport(List<DoctorStats> stats, DateTime period)
        {
            _stats  = stats;
            _period = period;
        }

        protected override string ReportTitle => "Raport Performanță Medici";

        protected override string GenerateIntroduction() =>
            $"  Luna: {_period:MMMM yyyy}\n" +
            $"  Medici evaluați: {_stats.Count}";

        protected override string GenerateBody()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("  STATISTICI INDIVIDUALE:");
            sb.AppendLine($"  {"Medic",-22} {"Appt",5} {"Fin",5} {"Revenue",10} {"AvgDur",8}");
            sb.AppendLine($"  {new string('-', 54)}");

            foreach (var s in _stats.OrderByDescending(s => s.TotalRevenue))
                sb.AppendLine(
                    $"  Dr.{s.Name,-19} {s.Appointments,5} {s.Completed,5} " +
                    $"{s.TotalRevenue,10:C} {s.AvgDurationMins,6:F0} min");

            return sb.ToString();
        }

        protected override string GenerateSummary()
        {
            var top = _stats.MaxBy(s => s.TotalRevenue);
            return $"  TOP PERFORMER: Dr.{top?.Name} – {top?.TotalRevenue:C}\n" +
                   $"  Total venituri echipă: {_stats.Sum(s => s.TotalRevenue):C}";
        }
    }
}
