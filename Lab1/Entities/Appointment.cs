using DentalClinic.Enums;
using DentalClinic.Interfaces;

namespace DentalClinic.Entities
{
    /// <summary>
    /// Reprezintă o programare în cadrul clinicii.
    /// SRP  – gestionează exclusiv datele și stările unei programări.
    /// OCP  – statusul poate fi extins cu noi valori în enum fără modificare.
    /// ISP  – implementează ISchedulable cu metode relevante programărilor.
    /// </summary>
    public class Appointment : IIdentifiable, ISchedulable
    {
        // ── Câmpuri private ────────────────────────────────────────────
        private static int _idCounter = 1;
        private DateTime   _dateTime;

        // ── Constructor ────────────────────────────────────────────────
        public Appointment(
            int           patientId,
            int           doctorId,
            DateTime      dateTime,
            TreatmentType treatmentType)
        {
            Id            = _idCounter++;
            PatientId     = patientId;
            DoctorId      = doctorId;
            TreatmentType = treatmentType;
            Status        = AppointmentStatus.Scheduled;
            CreatedAt     = DateTime.Now;
            Notes         = string.Empty;
            CancelReason  = string.Empty;

            Schedule(dateTime);
        }

        // ── Proprietăți ────────────────────────────────────────────────
        public int            Id            { get; }
        public int            PatientId     { get; }
        public int            DoctorId      { get; }
        public TreatmentType  TreatmentType { get; }
        public AppointmentStatus Status     { get; private set; }
        public DateTime       CreatedAt     { get; }
        public string         Notes         { get; set; }
        public string         CancelReason  { get; private set; }

        public DateTime DateTime
        {
            get => _dateTime;
            private set
            {
                if (value < System.DateTime.Now)
                    throw new ArgumentException("Programarea nu poate fi în trecut.");
                _dateTime = value;
            }
        }

        // ── ISchedulable ───────────────────────────────────────────────
        public void Schedule(DateTime dateTime)
        {
            if (Status == AppointmentStatus.Cancelled)
                throw new InvalidOperationException("Programarea anulată nu poate fi reprogramată.");

            DateTime = dateTime;
            Status   = AppointmentStatus.Scheduled;
        }

        public void Confirm()
        {
            if (Status != AppointmentStatus.Scheduled)
                throw new InvalidOperationException("Doar programările cu status 'Scheduled' pot fi confirmate.");

            Status = AppointmentStatus.Confirmed;
        }

        public void Cancel(string reason)
        {
            if (Status == AppointmentStatus.Completed)
                throw new InvalidOperationException("Nu se poate anula o programare deja finalizată.");

            Status       = AppointmentStatus.Cancelled;
            CancelReason = reason ?? "Motiv nespecificat";
        }

        /// <summary>Marchează programarea ca finalizată.</summary>
        public void Complete()
        {
            if (Status != AppointmentStatus.Confirmed)
                throw new InvalidOperationException("Doar programările confirmate pot fi finalizate.");

            Status = AppointmentStatus.Completed;
        }

        // ── Override ───────────────────────────────────────────────────
        public override string ToString() =>
            $"Programare #{Id} | {TreatmentType} | {DateTime:dd.MM.yyyy HH:mm} | Status: {Status}";
    }
}
