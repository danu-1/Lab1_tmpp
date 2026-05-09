using DentalClinic.Enums;
using DentalClinic.Interfaces;

namespace DentalClinic.Entities
{
    /// <summary>
    /// Înregistrarea unui tratament efectuat în cadrul unei programări.
    /// SRP – responsabilă exclusiv de istoricul medical al unui tratament.
    /// </summary>
    public class TreatmentRecord : IIdentifiable, ITreatmentRecordable
    {
        // ── Câmpuri private ────────────────────────────────────────────
        private static int    _idCounter = 1;
        private decimal       _cost;
        private readonly List<string> _notes = new();

        // ── Constructor ────────────────────────────────────────────────
        public TreatmentRecord(
            int           appointmentId,
            int           patientId,
            int           doctorId,
            TreatmentType treatmentType,
            string        diagnosis,
            string        procedure,
            decimal       cost)
        {
            Id            = _idCounter++;
            AppointmentId = appointmentId;
            PatientId     = patientId;
            DoctorId      = doctorId;
            TreatmentType = treatmentType;
            Diagnosis     = diagnosis;
            Procedure     = procedure;
            Cost          = cost;
            Date          = DateTime.Now;
        }

        // ── Proprietăți ────────────────────────────────────────────────
        public int           Id            { get; }
        public int           AppointmentId { get; }
        public int           PatientId     { get; }
        public int           DoctorId      { get; }
        public TreatmentType TreatmentType { get; }
        public string        Diagnosis     { get; }
        public string        Procedure     { get; }
        public DateTime      Date          { get; }

        public decimal Cost
        {
            get => _cost;
            private set
            {
                if (value < 0)
                    throw new ArgumentException("Costul tratamentului nu poate fi negativ.");
                _cost = value;
            }
        }

        /// <summary>Lista de note clinice adăugate pe parcurs.</summary>
        public IReadOnlyList<string> Notes => _notes.AsReadOnly();

        // ── ITreatmentRecordable ───────────────────────────────────────
        public void AddTreatmentNote(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
                _notes.Add($"[{DateTime.Now:dd.MM.yyyy HH:mm}] {note.Trim()}");
        }

        public string GetTreatmentSummary() =>
            $"Tratament: {TreatmentType}\n" +
            $"Diagnostic: {Diagnosis}\n"    +
            $"Procedură: {Procedure}\n"     +
            $"Cost: {Cost:C}\n"             +
            $"Data: {Date:dd.MM.yyyy}\n"    +
            $"Note: {(Notes.Count > 0 ? string.Join("; ", Notes) : "—")}";

        // ── Override ───────────────────────────────────────────────────
        public override string ToString() =>
            $"TreatmentRecord #{Id} | {TreatmentType} | {Date:dd.MM.yyyy} | Cost: {Cost:C}";
    }
}
