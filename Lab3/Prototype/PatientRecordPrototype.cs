// ═══════════════════════════════════════════════════════════════════════
//  PROTOTYPE PATTERN
//  Domeniu: Șabloane de fișe medicale și de tratament
//
//  Scenariu: Clinica menține un registru de prototipuri (template-uri)
//  pentru fișe medicale și planuri de tratament standard.
//  Când un medic creează un document similar unuia existent,
//  clonează prototipul și îl adaptează – mult mai rapid decât de la zero.
//
//  Deep Copy vs Shallow Copy:
//  – Shallow: câmpurile de tip valoare (int, decimal, string*) se copiază
//  – Deep:    listele și obiectele referință se copiază recursiv,
//             astfel încât modificările în clonă nu afectează originalul.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab3.Prototype
{
    // ───────────────────────────────────────────────────────────────────
    // 1. INTERFAȚA PROTOTYPE
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața de clonare. Orice document din sistem poate fi clonat.
    /// </summary>
    public interface ICloneable<T>
    {
        /// <summary>Copie superficială – câmpuri primitive.</summary>
        T ShallowClone();

        /// <summary>Copie profundă – inclusiv colecții și obiecte referință.</summary>
        T DeepClone();
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. PROTOTIP CONCRET – PatientRecord (Fișă medicală)
    // ───────────────────────────────────────────────────────────────────

    public class Allergy
    {
        public string Substance { get; set; } = string.Empty;
        public string Severity  { get; set; } = string.Empty;

        public Allergy() { }
        public Allergy(string substance, string severity)
        {
            Substance = substance;
            Severity  = severity;
        }

        // Clonă profundă a alergie (string este immutable, deci ok)
        public Allergy Clone() => new(Substance, Severity);

        public override string ToString() => $"{Substance} ({Severity})";
    }

    public class MedicalCondition
    {
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DiagnosedAt { get; set; }

        public MedicalCondition() { }
        public MedicalCondition(string name, string desc, DateTime at)
        {
            Name = name; Description = desc; DiagnosedAt = at;
        }

        public MedicalCondition Clone() => new(Name, Description, DiagnosedAt);

        public override string ToString() =>
            $"{Name} – {Description} ({DiagnosedAt:dd.MM.yyyy})";
    }

    /// <summary>
    /// Fișă medicală a pacientului – poate fi clonată ca șablon.
    /// </summary>
    public class PatientRecord : ICloneable<PatientRecord>
    {
        private static int _idCounter = 1;

        public int    RecordId     { get; private set; }
        public string PatientName  { get; set; } = string.Empty;
        public int    Age          { get; set; }
        public string BloodType    { get; set; } = string.Empty;
        public bool   IsSmoker     { get; set; }
        public bool   HasDiabetes  { get; set; }
        public string DentistNotes { get; set; } = string.Empty;

        public List<Allergy>         Allergies   { get; set; } = new();
        public List<MedicalCondition> Conditions { get; set; } = new();
        public List<string>          Medications { get; set; } = new();

        public PatientRecord() => RecordId = _idCounter++;

        // ── SHALLOW CLONE ─────────────────────────────────────────────
        /// <summary>
        /// Copie superficială: câmpurile primitive se copiază,
        /// DAR listele sunt aceleași referințe.
        /// Modificarea listelor în clonă afectează originalul!
        /// Util doar dacă știm că nu vom modifica colecțiile.
        /// </summary>
        public PatientRecord ShallowClone()
        {
            var clone = (PatientRecord)MemberwiseClone();
            clone.RecordId = _idCounter++;   // ID nou
            return clone;
        }

        // ── DEEP CLONE ────────────────────────────────────────────────
        /// <summary>
        /// Copie profundă: fiecare obiect din liste este clonat individual.
        /// Modificările în clonă NU afectează originalul.
        /// Recomandat pentru template-uri reutilizabile.
        /// </summary>
        public PatientRecord DeepClone()
        {
            var clone = new PatientRecord
            {
                PatientName  = PatientName,
                Age          = Age,
                BloodType    = BloodType,
                IsSmoker     = IsSmoker,
                HasDiabetes  = HasDiabetes,
                DentistNotes = DentistNotes,

                // Fiecare alergie este clonată independent
                Allergies  = Allergies.Select(a => a.Clone()).ToList(),
                Conditions = Conditions.Select(c => c.Clone()).ToList(),
                Medications = new List<string>(Medications)   // string e immutable
            };
            return clone;
        }

        public void Print()
        {
            Console.WriteLine($"\n  [Fișă #{RecordId}] {PatientName} | " +
                              $"Vârstă: {Age} | Grup sanguin: {BloodType}");
            Console.WriteLine($"  Fumător: {(IsSmoker ? "Da" : "Nu")} | " +
                              $"Diabet: {(HasDiabetes ? "Da" : "Nu")}");
            if (Allergies.Count > 0)
                Console.WriteLine($"  Alergii: {string.Join(", ", Allergies)}");
            if (Conditions.Count > 0)
                Console.WriteLine($"  Condiții: {string.Join(", ", Conditions)}");
            if (Medications.Count > 0)
                Console.WriteLine($"  Medicamente: {string.Join(", ", Medications)}");
            if (!string.IsNullOrWhiteSpace(DentistNotes))
                Console.WriteLine($"  Note: {DentistNotes}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. PROTOTIP CONCRET – TreatmentTemplate (Șablon de tratament)
    // ───────────────────────────────────────────────────────────────────

    public class TreatmentStep
    {
        public int    Order       { get; set; }
        public string Description { get; set; } = string.Empty;
        public int    DurationMins { get; set; }
        public decimal Cost        { get; set; }

        public TreatmentStep Clone() =>
            new() { Order = Order, Description = Description,
                    DurationMins = DurationMins, Cost = Cost };

        public override string ToString() =>
            $"  {Order}. {Description} ({DurationMins} min, {Cost:C})";
    }

    /// <summary>
    /// Șablon de tratament reutilizabil (ex: protocol extracție, detartraj).
    /// Clinica menține o bibliotecă de astfel de template-uri.
    /// </summary>
    public class TreatmentTemplate : ICloneable<TreatmentTemplate>
    {
        private static int _idCounter = 1;

        public int    TemplateId   { get; private set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Category     { get; set; } = string.Empty;
        public string Description  { get; set; } = string.Empty;
        public int    Version      { get; set; } = 1;
        public DateTime CreatedAt  { get; set; }
        public DateTime? LastUsed  { get; set; }

        public List<TreatmentStep> Steps     { get; set; } = new();
        public List<string>        Equipment { get; set; } = new();
        public List<string>        Warnings  { get; set; } = new();

        public TreatmentTemplate()
        {
            TemplateId = _idCounter++;
            CreatedAt  = DateTime.Now;
        }

        public decimal TotalCost => Steps.Sum(s => s.Cost);
        public int TotalMinutes  => Steps.Sum(s => s.DurationMins);

        // ── SHALLOW CLONE ─────────────────────────────────────────────
        public TreatmentTemplate ShallowClone()
        {
            var clone = (TreatmentTemplate)MemberwiseClone();
            clone.TemplateId = _idCounter++;
            clone.Version    = 1;
            clone.CreatedAt  = DateTime.Now;
            clone.LastUsed   = null;
            return clone;
        }

        // ── DEEP CLONE ────────────────────────────────────────────────
        public TreatmentTemplate DeepClone()
        {
            return new TreatmentTemplate
            {
                TemplateName = TemplateName,
                Category     = Category,
                Description  = Description,
                Version      = 1,
                // Liste complet independente
                Steps     = Steps.Select(s => s.Clone()).ToList(),
                Equipment = new List<string>(Equipment),
                Warnings  = new List<string>(Warnings)
            };
        }

        public void Print()
        {
            Console.WriteLine($"\n  [Template #{TemplateId} v{Version}] " +
                              $"{TemplateName} ({Category})");
            Console.WriteLine($"  {Description}");
            Console.WriteLine($"  Pași ({Steps.Count}), Total: {TotalMinutes} min, {TotalCost:C}");
            foreach (var s in Steps) Console.WriteLine(s);
            if (Equipment.Count > 0)
                Console.WriteLine($"  Echipament: {string.Join(", ", Equipment)}");
            if (Warnings.Count > 0)
                Console.WriteLine($"  Atenționări: {string.Join(" | ", Warnings)}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. REGISTRUL DE PROTOTIPURI – centralizează template-urile
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registrul stochează prototipurile și livrează clone la cerere.
    /// Clientul nu lucrează niciodată direct cu prototipul original.
    /// </summary>
    public class PrototypeRegistry
    {
        private readonly Dictionary<string, PatientRecord>     _recordTemplates   = new();
        private readonly Dictionary<string, TreatmentTemplate> _treatmentTemplates = new();

        // ── PatientRecord templates ────────────────────────────────────
        public void RegisterRecord(string key, PatientRecord template)
            => _recordTemplates[key] = template;

        /// <summary>Returnează o clonă profundă – originalul rămâne intact.</summary>
        public PatientRecord GetRecordClone(string key)
        {
            if (!_recordTemplates.TryGetValue(key, out var proto))
                throw new KeyNotFoundException($"Template '{key}' inexistent.");
            return proto.DeepClone();
        }

        // ── TreatmentTemplate templates ────────────────────────────────
        public void RegisterTemplate(string key, TreatmentTemplate template)
            => _treatmentTemplates[key] = template;

        public TreatmentTemplate GetTemplateClone(string key)
        {
            if (!_treatmentTemplates.TryGetValue(key, out var proto))
                throw new KeyNotFoundException($"Template '{key}' inexistent.");
            return proto.DeepClone();
        }

        public IEnumerable<string> ListRecordKeys()    => _recordTemplates.Keys;
        public IEnumerable<string> ListTemplateKeys()  => _treatmentTemplates.Keys;
    }
}
