// ═══════════════════════════════════════════════════════════════════════
//  PROXY PATTERN
//  Domeniu: Acces la dosarul medical al pacientului
//
//  Trei tipuri de Proxy implementate simultan:
//
//  1. VIRTUAL PROXY (Lazy Loading):
//     Dosarul medical complet conține imagini RX, istorice lungi.
//     Nu le încărcăm până nu sunt cerute explicit.
//     → PatientRecordProxy: creează RealPatientRecord doar la primul acces.
//
//  2. PROTECTION PROXY (Control acces pe roluri):
//     Nu toți utilizatorii pot vedea/modifica toate datele.
//     Recepționist: citește date contact.
//     Asistentă: citește fișa medicală, fără finanțe.
//     Medic: acces complet la dosar medical.
//     Administrator: acces complet inclusiv date financiare.
//     → ProtectionProxy: verifică rolul înainte de fiecare operație.
//
//  3. LOGGING PROXY (Audit):
//     Orice acces la dosarul medical trebuie jurnalizat (GDPR).
//     → LoggingProxy: înregistrează cine, când, ce operație.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab5.Proxy
{
    // ───────────────────────────────────────────────────────────────────
    // 1. INTERFAȚA SUBIECTULUI – contractul comun
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața pe care atât obiectul real cât și proxy-urile o implementează.
    /// Clientul nu știe dacă vorbește cu realul sau cu proxy-ul.
    /// </summary>
    public interface IPatientRecord
    {
        string  PatientId         { get; }
        string  GetBasicInfo();
        string  GetMedicalHistory();
        string  GetTreatmentRecords();
        string  GetFinancialSummary();
        void    AddTreatmentNote(string note, string doctorId);
        void    UpdateContactInfo(string phone, string email);
        bool    HasSensitivData   { get; }
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. SUBIECTUL REAL – obiectul "scump" de creat/accesat
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dosarul medical complet al pacientului.
    /// Simulăm că e "scump" de creat (DB queries, imagini RX, etc.)
    /// </summary>
    public class RealPatientRecord : IPatientRecord
    {
        public string PatientId { get; }
        public bool   HasSensitivData => true;

        private readonly string _name;
        private readonly string _birthDate;
        private string _phone;
        private string _email;

        private readonly List<string> _medicalHistory;
        private readonly List<string> _treatmentNotes;
        private readonly List<(decimal Amount, string Desc, DateTime Date)> _payments;

        public RealPatientRecord(string patientId, string name,
            string birthDate, string phone, string email)
        {
            PatientId = patientId;
            _name     = name;
            _birthDate = birthDate;
            _phone    = phone;
            _email    = email;

            // Simulăm date bogate (în realitate vin din DB)
            Console.WriteLine($"    [RealPatientRecord] Încarcare dosar {patientId} din DB... ✓");
            Thread.Sleep(50); // simulare latență DB

            _medicalHistory = new List<string>
            {
                "2021-03-10: Hipertensiune arterială – tratament cronic",
                "2022-07-15: Alergie penicilină – reacție moderată",
                "2023-01-20: Diabet tip II – controlat medicamentos"
            };
            _treatmentNotes = new List<string>
            {
                "2023-06-01: Detartraj ultrasonic – fără complicații",
                "2023-09-15: Obturație M46 compozit A2 – pacient cooperant",
                "2024-02-20: Extracție 38 – sutură, antibioterapie 7 zile"
            };
            _payments = new List<(decimal, string, DateTime)>
            {
                (650m, "Obturație + Detartraj", new DateTime(2023,9,15)),
                (450m, "Extracție + Consultație", new DateTime(2024,2,20))
            };
        }

        public string GetBasicInfo() =>
            $"ID: {PatientId} | Nume: {_name} | " +
            $"Născut: {_birthDate} | Tel: {_phone} | Email: {_email}";

        public string GetMedicalHistory() =>
            $"Istoric medical ({_medicalHistory.Count} intrări):\n" +
            string.Join("\n", _medicalHistory.Select(h => $"  • {h}"));

        public string GetTreatmentRecords() =>
            $"Înregistrări tratamente ({_treatmentNotes.Count}):\n" +
            string.Join("\n", _treatmentNotes.Select(t => $"  • {t}"));

        public string GetFinancialSummary()
        {
            decimal total = _payments.Sum(p => p.Amount);
            return $"Sumar financiar (total: {total:C}):\n" +
                string.Join("\n", _payments.Select(
                    p => $"  • {p.Date:dd.MM.yyyy}: {p.Desc} – {p.Amount:C}"));
        }

        public void AddTreatmentNote(string note, string doctorId)
        {
            _treatmentNotes.Add($"{DateTime.Now:yyyy-MM-dd}: {note} [Dr.{doctorId}]");
            Console.WriteLine($"    [RealPatientRecord] Notă adăugată de Dr.{doctorId}");
        }

        public void UpdateContactInfo(string phone, string email)
        {
            _phone = phone;
            _email = email;
            Console.WriteLine($"    [RealPatientRecord] Contact actualizat: {phone}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. VIRTUAL PROXY – Lazy Loading
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Virtual Proxy: amână crearea RealPatientRecord până la
    /// primul apel efectiv. Util când avem o listă cu sute de
    /// dosare – nu le încărcăm pe toate din start.
    /// </summary>
    public class LazyPatientRecordProxy : IPatientRecord
    {
        private readonly string _patientId;
        private readonly string _name;
        private readonly string _birthDate;
        private readonly string _phone;
        private readonly string _email;

        // Obiectul real – null până la primul acces
        private RealPatientRecord? _realRecord;
        private bool _isLoaded = false;

        public LazyPatientRecordProxy(string patientId, string name,
            string birthDate, string phone, string email)
        {
            _patientId = patientId;
            _name      = name;
            _birthDate = birthDate;
            _phone     = phone;
            _email     = email;
            Console.WriteLine($"    [LazyProxy] Proxy creat pentru {patientId} – fără încărcare DB");
        }

        public string PatientId      => _patientId;
        public bool   HasSensitivData => true;

        // Lazy initialization
        private RealPatientRecord GetReal()
        {
            if (!_isLoaded)
            {
                Console.WriteLine($"    [LazyProxy] Primul acces → încărcare din DB...");
                _realRecord = new RealPatientRecord(_patientId, _name, _birthDate, _phone, _email);
                _isLoaded   = true;
            }
            return _realRecord!;
        }

        public string GetBasicInfo()         => GetReal().GetBasicInfo();
        public string GetMedicalHistory()    => GetReal().GetMedicalHistory();
        public string GetTreatmentRecords()  => GetReal().GetTreatmentRecords();
        public string GetFinancialSummary()  => GetReal().GetFinancialSummary();

        public void AddTreatmentNote(string note, string doctorId)
            => GetReal().AddTreatmentNote(note, doctorId);

        public void UpdateContactInfo(string phone, string email)
            => GetReal().UpdateContactInfo(phone, email);

        public bool IsLoaded => _isLoaded;
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. PROTECTION PROXY – Control acces bazat pe roluri
    // ───────────────────────────────────────────────────────────────────

    public enum UserRole
    {
        Receptionist,   // date contact, programări
        Nurse,          // fișă medicală, tratamente
        Doctor,         // acces complet medical
        Administrator   // acces complet inclusiv financiar
    }

    public class SystemUser
    {
        public string   UserId   { get; init; } = string.Empty;
        public string   Name     { get; init; } = string.Empty;
        public UserRole Role     { get; init; }
        public override string ToString() => $"{Name} ({Role})";
    }

    /// <summary>
    /// Protection Proxy: verifică drepturile utilizatorului curent
    /// înainte de a permite accesul la fiecare operație.
    /// </summary>
    public class ProtectionProxy : IPatientRecord
    {
        private readonly IPatientRecord _realRecord;
        private readonly SystemUser     _currentUser;

        public ProtectionProxy(IPatientRecord realRecord, SystemUser currentUser)
        {
            _realRecord  = realRecord;
            _currentUser = currentUser;
        }

        public string PatientId      => _realRecord.PatientId;
        public bool   HasSensitivData => _realRecord.HasSensitivData;

        // Recepționist și mai sus pot vedea date de bază
        public string GetBasicInfo()
        {
            AssertRole(UserRole.Receptionist, "GetBasicInfo");
            return _realRecord.GetBasicInfo();
        }

        // Asistentă și mai sus pot vedea istoricul medical
        public string GetMedicalHistory()
        {
            AssertRole(UserRole.Nurse, "GetMedicalHistory");
            return _realRecord.GetMedicalHistory();
        }

        // Asistentă și mai sus pot vedea tratamentele
        public string GetTreatmentRecords()
        {
            AssertRole(UserRole.Nurse, "GetTreatmentRecords");
            return _realRecord.GetTreatmentRecords();
        }

        // Doar Administrator vede datele financiare
        public string GetFinancialSummary()
        {
            AssertRole(UserRole.Administrator, "GetFinancialSummary");
            return _realRecord.GetFinancialSummary();
        }

        // Doar Medic sau Administrator pot adăuga note
        public void AddTreatmentNote(string note, string doctorId)
        {
            AssertRole(UserRole.Doctor, "AddTreatmentNote");
            _realRecord.AddTreatmentNote(note, doctorId);
        }

        // Recepționist și mai sus pot actualiza contactul
        public void UpdateContactInfo(string phone, string email)
        {
            AssertRole(UserRole.Receptionist, "UpdateContactInfo");
            _realRecord.UpdateContactInfo(phone, email);
        }

        private void AssertRole(UserRole minimumRole, string operation)
        {
            if (_currentUser.Role < minimumRole)
                throw new UnauthorizedAccessException(
                    $"[ProtectionProxy] ACCES REFUZAT: {_currentUser} " +
                    $"nu are drept pentru '{operation}'. " +
                    $"Necesar: {minimumRole}+, Actual: {_currentUser.Role}");

            Console.WriteLine($"    [ProtectionProxy] ✅ {_currentUser} → '{operation}' permis");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. LOGGING PROXY – Audit GDPR
    // ───────────────────────────────────────────────────────────────────

    public class AccessLogEntry
    {
        public DateTime Timestamp  { get; init; } = DateTime.Now;
        public string   UserId     { get; init; } = string.Empty;
        public string   PatientId  { get; init; } = string.Empty;
        public string   Operation  { get; init; } = string.Empty;
        public bool     Success    { get; init; }
        public string   Details    { get; init; } = string.Empty;

        public override string ToString() =>
            $"[{Timestamp:HH:mm:ss}] {UserId} → {PatientId}.{Operation} " +
            $"[{(Success ? "OK" : "FAIL")}] {Details}";
    }

    /// <summary>
    /// Logging Proxy: jurnalizează orice acces la dosar (audit GDPR).
    /// Poate fi combinat cu ProtectionProxy (chain of proxies).
    /// </summary>
    public class LoggingProxy : IPatientRecord
    {
        private readonly IPatientRecord      _inner;
        private readonly string              _userId;
        private readonly List<AccessLogEntry> _log = new();

        public LoggingProxy(IPatientRecord inner, string userId)
        {
            _inner  = inner;
            _userId = userId;
        }

        public string PatientId       => _inner.PatientId;
        public bool   HasSensitivData => _inner.HasSensitivData;

        public string GetBasicInfo()        => Execute("GetBasicInfo",
            () => _inner.GetBasicInfo());
        public string GetMedicalHistory()   => Execute("GetMedicalHistory",
            () => _inner.GetMedicalHistory());
        public string GetTreatmentRecords() => Execute("GetTreatmentRecords",
            () => _inner.GetTreatmentRecords());
        public string GetFinancialSummary() => Execute("GetFinancialSummary",
            () => _inner.GetFinancialSummary());

        public void AddTreatmentNote(string note, string doctorId)
            => ExecuteVoid("AddTreatmentNote",
                () => _inner.AddTreatmentNote(note, doctorId), $"Dr:{doctorId}");

        public void UpdateContactInfo(string phone, string email)
            => ExecuteVoid("UpdateContactInfo",
                () => _inner.UpdateContactInfo(phone, email));

        // ── Helpers ────────────────────────────────────────────────────
        private string Execute(string op, Func<string> action)
        {
            try
            {
                string result = action();
                Log(op, true);
                return result;
            }
            catch (Exception ex)
            {
                Log(op, false, ex.Message);
                throw;
            }
        }

        private void ExecuteVoid(string op, Action action, string details = "")
        {
            try
            {
                action();
                Log(op, true, details);
            }
            catch (Exception ex)
            {
                Log(op, false, ex.Message);
                throw;
            }
        }

        private void Log(string op, bool success, string details = "")
        {
            var entry = new AccessLogEntry
            {
                UserId    = _userId,
                PatientId = PatientId,
                Operation = op,
                Success   = success,
                Details   = details
            };
            _log.Add(entry);
            Console.WriteLine($"    [LoggingProxy] {entry}");
        }

        public IReadOnlyList<AccessLogEntry> GetAuditLog() => _log.AsReadOnly();
        public int LogCount => _log.Count;
    }
}
