using DentalClinic.Enums;
using DentalClinic.Interfaces;

namespace DentalClinic.Entities
{
    /// <summary>
    /// Reprezintă o plată pentru o programare.
    /// SRP  – gestionează exclusiv logica financiară a unei plăți.
    /// ISP  – implementează IPayable cu metode relevante plăților.
    /// </summary>
    public class Payment : IIdentifiable, IPayable
    {
        // ── Câmpuri private ────────────────────────────────────────────
        private static int _idCounter = 1;
        private decimal    _amountPaid;

        // ── Constructor ────────────────────────────────────────────────
        public Payment(int appointmentId, int patientId, decimal totalAmount)
        {
            if (totalAmount <= 0)
                throw new ArgumentException("Suma totală trebuie să fie pozitivă.");

            Id            = _idCounter++;
            AppointmentId = appointmentId;
            PatientId     = patientId;
            TotalAmount   = totalAmount;
            _amountPaid   = 0;
            Status        = PaymentStatus.Pending;
            CreatedAt     = DateTime.Now;
            Transactions  = new List<PaymentTransaction>();
        }

        // ── Proprietăți ────────────────────────────────────────────────
        public int           Id            { get; }
        public int           AppointmentId { get; }
        public int           PatientId     { get; }
        public decimal       TotalAmount   { get; }
        public decimal       AmountPaid    => _amountPaid;
        public PaymentStatus Status        { get; private set; }
        public DateTime      CreatedAt     { get; }

        /// <summary>Istoricul tranzacțiilor pentru acest payment.</summary>
        public List<PaymentTransaction> Transactions { get; }

        // ── IPayable ───────────────────────────────────────────────────
        public void ProcessPayment(decimal amount, PaymentMethod method)
        {
            if (amount <= 0)
                throw new ArgumentException("Suma trebuie să fie pozitivă.");
            if (Status == PaymentStatus.Paid)
                throw new InvalidOperationException("Plata a fost deja achitată integral.");
            if (Status == PaymentStatus.Refunded)
                throw new InvalidOperationException("Plata a fost rambursată.");

            _amountPaid += amount;

            Transactions.Add(new PaymentTransaction(amount, method, TransactionType.Payment));

            Status = _amountPaid >= TotalAmount
                ? PaymentStatus.Paid
                : PaymentStatus.PartiallyPaid;
        }

        public void RefundPayment(decimal amount)
        {
            if (amount <= 0 || amount > _amountPaid)
                throw new ArgumentException("Suma de rambursat este invalidă.");

            _amountPaid -= amount;
            Transactions.Add(new PaymentTransaction(amount, PaymentMethod.Cash, TransactionType.Refund));

            Status = _amountPaid <= 0
                ? PaymentStatus.Refunded
                : PaymentStatus.PartiallyPaid;
        }

        public decimal GetRemainingBalance() => TotalAmount - _amountPaid;

        // ── Override ───────────────────────────────────────────────────
        public override string ToString() =>
            $"Payment #{Id} | Total: {TotalAmount:C} | Plătit: {AmountPaid:C} | Status: {Status}";
    }

    // ──────────────────────────────────────────────────────────────────
    // Value Object: o tranzacție individuală (plată sau rambursare)
    // ──────────────────────────────────────────────────────────────────
    public enum TransactionType { Payment, Refund }

    public class PaymentTransaction
    {
        public PaymentTransaction(decimal amount, PaymentMethod method, TransactionType type)
        {
            Amount    = amount;
            Method    = method;
            Type      = type;
            Timestamp = DateTime.Now;
        }

        public decimal          Amount    { get; }
        public PaymentMethod    Method    { get; }
        public TransactionType  Type      { get; }
        public DateTime         Timestamp { get; }

        public override string ToString() =>
            $"[{Timestamp:dd.MM.yyyy HH:mm}] {Type}: {Amount:C} via {Method}";
    }
}
