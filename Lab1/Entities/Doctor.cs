using DentalClinic.Enums;

namespace DentalClinic.Entities
{
    /// <summary>
    /// Reprezintă un medic stomatolog al clinicii.
    /// SRP – gestionează exclusiv datele și disponibilitatea medicului.
    /// LSP – poate înlocui Person fără a afecta funcționalitatea.
    /// </summary>
    public class Doctor : Person
    {
        // ── Câmpuri private ────────────────────────────────────────────
        private decimal _consultationFee;

        // ── Constructor ────────────────────────────────────────────────
        public Doctor(
            string               firstName,
            string               lastName,
            string               phone,
            string               email,
            DoctorSpecialization specialization,
            decimal              consultationFee)
            : base(firstName, lastName, phone, email)
        {
            Specialization  = specialization;
            ConsultationFee = consultationFee;
            WorkingHours    = new Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)>();
        }

        // ── Proprietăți ────────────────────────────────────────────────
        public DoctorSpecialization Specialization { get; }

        public decimal ConsultationFee
        {
            get => _consultationFee;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Tariful nu poate fi negativ.");
                _consultationFee = value;
            }
        }

        /// <summary>Program de lucru: zi → (oră start, oră sfârșit).</summary>
        public Dictionary<DayOfWeek, (TimeSpan Start, TimeSpan End)> WorkingHours { get; }

        // ── Metode publice ─────────────────────────────────────────────
        /// <summary>Setează orarul pentru o zi a săptămânii.</summary>
        public void SetWorkingHours(DayOfWeek day, TimeSpan start, TimeSpan end)
        {
            if (end <= start)
                throw new ArgumentException("Ora de sfârșit trebuie să fie după ora de start.");
            WorkingHours[day] = (start, end);
        }

        /// <summary>Verifică dacă medicul lucrează la un moment dat.</summary>
        public bool IsAvailable(DateTime dateTime)
        {
            if (!WorkingHours.TryGetValue(dateTime.DayOfWeek, out var hours))
                return false;

            var time = dateTime.TimeOfDay;
            return time >= hours.Start && time <= hours.End;
        }

        // ── Polimorfism ────────────────────────────────────────────────
        public override string GetRole() => $"Dr. ({Specialization})";

        public override string ToString() =>
            $"{base.ToString()} | Specializare: {Specialization} | Tarif: {ConsultationFee:C}";
    }
}
