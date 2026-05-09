using DentalClinic.Entities;

namespace DentalClinic.Entities
{
    /// <summary>
    /// Reprezintă un pacient al clinicii.
    /// SRP – gestionează exclusiv datele și comportamentul pacientului.
    /// LSP – poate înlocui Person oriunde fără a rupe funcționalitatea.
    /// </summary>
    public class Patient : Person
    {
        // ── Câmpuri private ────────────────────────────────────────────
        private DateTime _dateOfBirth;
        private string   _address = string.Empty;

        // ── Constructor ────────────────────────────────────────────────
        public Patient(
            string   firstName,
            string   lastName,
            string   phone,
            string   email,
            DateTime dateOfBirth,
            string   address)
            : base(firstName, lastName, phone, email)
        {
            DateOfBirth = dateOfBirth;
            Address     = address;
            MedicalNotes = new List<string>();
        }

        // ── Proprietăți ────────────────────────────────────────────────
        public DateTime DateOfBirth
        {
            get => _dateOfBirth;
            private set
            {
                if (value >= DateTime.Today)
                    throw new ArgumentException("Data nașterii trebuie să fie în trecut.");
                _dateOfBirth = value;
            }
        }

        public string Address
        {
            get => _address;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Adresa nu poate fi goală.");
                _address = value.Trim();
            }
        }

        /// <summary>Alergii și condiții medicale importante.</summary>
        public List<string> MedicalNotes { get; }

        /// <summary>Vârsta calculată dinamic.</summary>
        public int Age
        {
            get
            {
                int age = DateTime.Today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > DateTime.Today.AddYears(-age)) age--;
                return age;
            }
        }

        // ── Metode publice ─────────────────────────────────────────────
        public void AddMedicalNote(string note)
        {
            if (!string.IsNullOrWhiteSpace(note))
                MedicalNotes.Add($"[{DateTime.Now:dd.MM.yyyy}] {note.Trim()}");
        }

        // ── Polimorfism ────────────────────────────────────────────────
        public override string GetRole() => "Pacient";

        public override string ToString() =>
            $"{base.ToString()} | Vârstă: {Age} ani | Adresă: {Address}";
    }
}
