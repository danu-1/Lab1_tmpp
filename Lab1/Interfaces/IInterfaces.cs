using DentalClinic.Enums;

namespace DentalClinic.Interfaces
{
    // ──────────────────────────────────────────────────────────────────
    // ISP: interfețe mici și specifice în loc de una mare "god interface"
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Orice entitate care poate fi identificată unic.
    /// </summary>
    public interface IIdentifiable
    {
        int Id { get; }
    }

    /// <summary>
    /// Orice entitate care are un nume afișabil.
    /// </summary>
    public interface INameable
    {
        string FullName { get; }
    }

    /// <summary>
    /// Orice entitate care poate fi contactată.
    /// </summary>
    public interface IContactable
    {
        string Phone { get; }
        string Email { get; }
    }

    // ──────────────────────────────────────────────────────────────────
    // Interfețe pentru operații de domeniu (SRP + ISP)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Comportament specific programărilor.
    /// </summary>
    public interface ISchedulable
    {
        void Schedule(DateTime dateTime);
        void Cancel(string reason);
        void Confirm();
    }

    /// <summary>
    /// Comportament specific plăților.
    /// </summary>
    public interface IPayable
    {
        void ProcessPayment(decimal amount, PaymentMethod method);
        void RefundPayment(decimal amount);
        decimal GetRemainingBalance();
    }

    /// <summary>
    /// Comportament specific tratamentelor.
    /// </summary>
    public interface ITreatmentRecordable
    {
        void AddTreatmentNote(string note);
        string GetTreatmentSummary();
    }

    // ──────────────────────────────────────────────────────────────────
    // Interfețe pentru repository (DIP – depindem de abstractizări)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Operații CRUD generice pentru orice entitate.
    /// DIP: modulele de nivel înalt nu depind de implementări concrete.
    /// </summary>
    public interface IRepository<T> where T : class, IIdentifiable
    {
        void Add(T entity);
        T? GetById(int id);
        IEnumerable<T> GetAll();
        void Update(T entity);
        void Delete(int id);
    }

    // ──────────────────────────────────────────────────────────────────
    // Interfețe pentru servicii (DIP)
    // ──────────────────────────────────────────────────────────────────

    public interface IAppointmentService
    {
        void BookAppointment(int patientId, int doctorId, DateTime dateTime, TreatmentType treatmentType);
        void CancelAppointment(int appointmentId, string reason);
        IEnumerable<object> GetAppointmentsByPatient(int patientId);
        IEnumerable<object> GetAppointmentsByDoctor(int doctorId, DateTime date);
    }

    public interface IPaymentService
    {
        void ProcessPayment(int appointmentId, decimal amount, PaymentMethod method);
        decimal GetTotalRevenue(DateTime from, DateTime to);
        IEnumerable<object> GetOverduePayments();
    }

    public interface ITreatmentService
    {
        void RecordTreatment(int appointmentId, string diagnosis, string procedure, decimal cost);
        IEnumerable<object> GetTreatmentHistory(int patientId);
    }

    // ──────────────────────────────────────────────────────────────────
    // Interfață pentru notificări (OCP – extensibil fără modificare)
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Orice canal de notificare implementează această interfață.
    /// Putem adăuga SMS, Push, WhatsApp fără a modifica codul existent.
    /// </summary>
    public interface INotificationSender
    {
        void Send(string recipient, string subject, string body);
    }
}
