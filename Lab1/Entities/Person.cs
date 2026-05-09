using DentalClinic.Interfaces;

namespace DentalClinic.Entities
{
    /// <summary>
    /// Clasă de bază abstractă pentru toate persoanele din sistem.
    /// SRP  – responsabilă doar de datele personale de bază.
    /// OCP  – poate fi extinsă (Patient, Doctor) fără modificare.
    /// LSP  – subclasele pot înlocui Person oriunde e necesar.
    /// </summary>
    public abstract class Person : IIdentifiable, INameable, IContactable
    {
        // ── Câmpuri private (Encapsulare) ──────────────────────────────
        private static int _idCounter = 1;

        private int    _id;
        private string _firstName = string.Empty;
        private string _lastName  = string.Empty;
        private string _phone     = string.Empty;
        private string _email     = string.Empty;

        // ── Constructori ───────────────────────────────────────────────
        protected Person(string firstName, string lastName, string phone, string email)
        {
            _id        = _idCounter++;
            FirstName  = firstName;
            LastName   = lastName;
            Phone      = phone;
            Email      = email;
            CreatedAt  = DateTime.Now;
        }

        // ── Proprietăți (Encapsulare cu validare) ──────────────────────
        public int Id => _id;

        public string FirstName
        {
            get => _firstName;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Prenumele nu poate fi gol.");
                _firstName = value.Trim();
            }
        }

        public string LastName
        {
            get => _lastName;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Numele nu poate fi gol.");
                _lastName = value.Trim();
            }
        }

        public string Phone
        {
            get => _phone;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentException("Telefonul nu poate fi gol.");
                _phone = value.Trim();
            }
        }

        public string Email
        {
            get => _email;
            protected set
            {
                if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
                    throw new ArgumentException("Email invalid.");
                _email = value.Trim();
            }
        }

        public DateTime CreatedAt { get; }

        // ── INameable ──────────────────────────────────────────────────
        public string FullName => $"{FirstName} {LastName}";

        // ── Metodă abstractă (Polimorfism) ─────────────────────────────
        /// <summary>
        /// Fiecare subclasă descrie rolul persoanei în sistem.
        /// </summary>
        public abstract string GetRole();

        // ── Override ToString ──────────────────────────────────────────
        public override string ToString() =>
            $"[{GetRole()}] {FullName} | Tel: {Phone} | Email: {Email}";
    }
}
