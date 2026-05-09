// ═══════════════════════════════════════════════════════════════════════
//  SINGLETON PATTERN
//  Domeniu: Configurarea clinicii și Jurnalul de audit
//
//  Două Singleton-uri distincte, fiecare cu un scop clar (SRP):
//
//  1. ClinicConfiguration – setările globale ale clinicii
//     (nume, orar, tarife, limite). O singură instanță garantează
//     că toate modulele citesc aceeași configurare.
//
//  2. AuditLog – jurnalul de evenimente al sistemului.
//     Thread-safe cu double-checked locking, garantând că
//     toate evenimentele sunt scrise în aceeași instanță.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab3.Singleton
{
    // ───────────────────────────────────────────────────────────────────
    // 1. SINGLETON – Configurarea clinicii
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configurarea globală a clinicii stomatologice.
    /// Thread-safe prin Lazy&lt;T&gt; – instanțierea e amânată
    /// până la primul acces și este garantat thread-safe de CLR.
    /// </summary>
    public sealed class ClinicConfiguration
    {
        // ── Implementare Singleton cu Lazy<T> (thread-safe, simplu) ───
        private static readonly Lazy<ClinicConfiguration> _instance =
            new(() => new ClinicConfiguration());

        /// <summary>Punctul de acces global la instanța unică.</summary>
        public static ClinicConfiguration Instance => _instance.Value;

        // ── Constructor privat – nimeni din afară nu poate instanția ───
        private ClinicConfiguration()
        {
            LoadDefaults();
        }

        // ── Proprietăți de configurare ─────────────────────────────────
        public string   ClinicName        { get; private set; } = string.Empty;
        public string   Address           { get; private set; } = string.Empty;
        public string   Phone             { get; private set; } = string.Empty;
        public string   Email             { get; private set; } = string.Empty;
        public string   LicenseNumber     { get; private set; } = string.Empty;

        public TimeSpan OpeningTime       { get; private set; }
        public TimeSpan ClosingTime       { get; private set; }
        public int      AppointmentSlotMins { get; private set; }
        public int      MaxDailyPatients  { get; private set; }

        public decimal  ConsultationBaseFee { get; private set; }
        public decimal  EmergencyFeeMultiplier { get; private set; }
        public decimal  VatRate           { get; private set; }

        public string   CurrencySymbol    { get; private set; } = string.Empty;
        public string   DateFormat        { get; private set; } = string.Empty;

        public bool     SendEmailReminders { get; private set; }
        public int      ReminderHoursBefore { get; private set; }

        // ── Metode de configurare (doar la inițializare / admin) ───────
        private void LoadDefaults()
        {
            ClinicName           = "DentaCare Clinic SRL";
            Address              = "Bd. Ștefan cel Mare, 100, Chișinău, MD-2001";
            Phone                = "+373 22 123 456";
            Email                = "contact@dentacare.md";
            LicenseNumber        = "MS-2024-0042";

            OpeningTime          = new TimeSpan(8, 0, 0);
            ClosingTime          = new TimeSpan(18, 0, 0);
            AppointmentSlotMins  = 30;
            MaxDailyPatients     = 40;

            ConsultationBaseFee  = 200m;
            EmergencyFeeMultiplier = 1.5m;
            VatRate              = 0.20m;

            CurrencySymbol       = "MDL";
            DateFormat           = "dd.MM.yyyy";

            SendEmailReminders   = true;
            ReminderHoursBefore  = 24;
        }

        /// <summary>Actualizare orar (apelat de admin).</summary>
        public void UpdateWorkingHours(TimeSpan open, TimeSpan close)
        {
            if (close <= open)
                throw new ArgumentException("Ora de închidere trebuie să fie după cea de deschidere.");
            OpeningTime  = open;
            ClosingTime  = close;
            AuditLog.Instance.Log("CONFIG",
                $"Orar actualizat: {open:hh\\:mm} – {close:hh\\:mm}");
        }

        /// <summary>Actualizare tarif de bază.</summary>
        public void UpdateBaseFee(decimal fee)
        {
            if (fee <= 0)
                throw new ArgumentException("Tariful trebuie să fie pozitiv.");
            ConsultationBaseFee = fee;
            AuditLog.Instance.Log("CONFIG",
                $"Tarif consultație actualizat: {fee:C}");
        }

        public void Print()
        {
            Console.WriteLine($"\n  Clinică    : {ClinicName}");
            Console.WriteLine($"  Adresă     : {Address}");
            Console.WriteLine($"  Telefon    : {Phone}");
            Console.WriteLine($"  Orar       : {OpeningTime:hh\\:mm} – {ClosingTime:hh\\:mm}");
            Console.WriteLine($"  Slot appt  : {AppointmentSlotMins} min");
            Console.WriteLine($"  Max/zi     : {MaxDailyPatients} pacienți");
            Console.WriteLine($"  Tarif bază : {ConsultationBaseFee:C}");
            Console.WriteLine($"  TVA        : {VatRate * 100:F0}%");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. SINGLETON – Jurnal de audit (thread-safe cu double-check lock)
    // ───────────────────────────────────────────────────────────────────

    public class AuditEntry
    {
        public DateTime Timestamp { get; init; }
        public string   Module    { get; init; } = string.Empty;
        public string   Message   { get; init; } = string.Empty;
        public int      ThreadId  { get; init; }

        public override string ToString() =>
            $"[{Timestamp:dd.MM.yyyy HH:mm:ss}][T{ThreadId:D2}][{Module,-10}] {Message}";
    }

    /// <summary>
    /// Jurnal de audit global al sistemului.
    /// Thread-safe prin double-checked locking cu volatile.
    /// Demonstrează Singleton în mediu multi-threaded.
    /// </summary>
    public sealed class AuditLog
    {
        // ── Double-checked locking pattern ────────────────────────────
        private static volatile AuditLog? _instance;
        private static readonly object    _lock = new();

        public static AuditLog Instance
        {
            get
            {
                if (_instance == null)                    // prima verificare (fără lock)
                {
                    lock (_lock)
                    {
                        if (_instance == null)            // a doua verificare (cu lock)
                            _instance = new AuditLog();
                    }
                }
                return _instance;
            }
        }

        private AuditLog()
        {
            _entries = new List<AuditEntry>();
        }

        // ── Stocare entries (lock la scriere) ─────────────────────────
        private readonly List<AuditEntry> _entries;
        private readonly object           _entriesLock = new();

        /// <summary>Înregistrează un eveniment în jurnal (thread-safe).</summary>
        public void Log(string module, string message)
        {
            var entry = new AuditEntry
            {
                Timestamp = DateTime.Now,
                Module    = module,
                Message   = message,
                ThreadId  = Environment.CurrentManagedThreadId
            };

            lock (_entriesLock)
            {
                _entries.Add(entry);
            }

            Console.WriteLine($"  [AUDIT] {entry}");
        }

        /// <summary>Returnează toate înregistrările (thread-safe).</summary>
        public IReadOnlyList<AuditEntry> GetEntries()
        {
            lock (_entriesLock)
            {
                return _entries.ToList().AsReadOnly();
            }
        }

        /// <summary>Filtrare după modul.</summary>
        public IEnumerable<AuditEntry> GetByModule(string module)
        {
            lock (_entriesLock)
            {
                return _entries
                    .Where(e => e.Module.Equals(module, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
        }

        /// <summary>Numărul total de intrări.</summary>
        public int Count
        {
            get { lock (_entriesLock) { return _entries.Count; } }
        }

        public void PrintAll()
        {
            lock (_entriesLock)
            {
                Console.WriteLine($"\n  Jurnal de audit ({_entries.Count} intrări):");
                foreach (var e in _entries)
                    Console.WriteLine($"  {e}");
            }
        }
    }
}
