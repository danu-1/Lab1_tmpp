// ═══════════════════════════════════════════════════════════════════════
//  FAÇADE PATTERN
//  Domeniu: Rezervarea unei programări stomatologice
//
//  Scenariu: Procesul de rezervare implică mai multe subsisteme interne:
//  verificarea disponibilității doctorului, validarea pacientului,
//  construirea planului de tratament, procesarea plății avansului,
//  trimiterea confirmărilor și înregistrarea în jurnalul de audit.
//
//  Fațada (AppointmentFacade) expune o singură metodă simplă:
//      BookAppointment(request) → AppointmentConfirmation
//  ascunzând complet complexitatea internă față de client.
//
//  Participanți:
//  – Façade       : AppointmentFacade
//  – Subsisteme   : SchedulerService, PatientValidationService,
//                   NotificationService, InvoiceService,
//                   (+ IPaymentProcessor din Adapter, AuditLog din Lab3)
// ═══════════════════════════════════════════════════════════════════════

using DentalClinic.Lab4.Singleton;
using DentalClinic.Lab4.Adapter;
using DentalClinic.Lab4.Composite;

namespace DentalClinic.Lab4.Facade
{
    // ───────────────────────────────────────────────────────────────────
    // 1. DATA TRANSFER OBJECTS (cerere și confirmare)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Datele trimise de client la rezervarea programării.</summary>
    public class AppointmentRequest
    {
        public string                   PatientName    { get; init; } = string.Empty;
        public int                      PatientAge     { get; init; }
        public string                   PatientPhone   { get; init; } = string.Empty;
        public string                   PatientEmail   { get; init; } = string.Empty;
        public string                   DoctorName     { get; init; } = string.Empty;
        public string                   DoctorSpecialty { get; init; } = string.Empty;
        public DateTime                 DesiredDateTime { get; init; }
        public IDentalServiceComponent  Service        { get; init; } = null!;
        public PaymentProviderType      PaymentMethod  { get; init; } = PaymentProviderType.Maib;
        public bool                     PayDepositNow  { get; init; } = true;
        public string                   Notes          { get; init; } = string.Empty;
    }

    /// <summary>Confirmarea returnată clientului după rezervare reușită.</summary>
    public class AppointmentConfirmation
    {
        public bool     Success         { get; init; }
        public string   BookingId       { get; init; } = string.Empty;
        public string   PatientName     { get; init; } = string.Empty;
        public string   DoctorName      { get; init; } = string.Empty;
        public DateTime AppointmentTime { get; init; }
        public string   ServiceName     { get; init; } = string.Empty;
        public decimal  TotalPrice      { get; init; }
        public decimal  DepositPaid     { get; init; }
        public decimal  BalanceDue      { get; init; }
        public string   TransactionId   { get; init; } = string.Empty;
        public string   ConfirmationMsg { get; init; } = string.Empty;
        public string   ErrorMessage    { get; init; } = string.Empty;

        public void Print()
        {
            Console.WriteLine(Success
                ? $"\n  ╔══════════════════════════════════════════════╗"
                : $"\n  ╔═══════════════════════ EROARE ═══════════════╗");

            if (Success)
            {
                Console.WriteLine($"  ║  CONFIRMARE PROGRAMARE #{BookingId,-24}║");
                Console.WriteLine($"  ╠══════════════════════════════════════════════╣");
                Console.WriteLine($"  ║  Pacient   : {PatientName,-32}║");
                Console.WriteLine($"  ║  Medic     : {DoctorName,-32}║");
                Console.WriteLine($"  ║  Data/Ora  : {AppointmentTime:dd.MM.yyyy HH:mm,-32}║");
                Console.WriteLine($"  ║  Serviciu  : {ServiceName,-32}║");
                Console.WriteLine($"  ║  Preț total: {TotalPrice,7:F2} MDL{"",-22}║");
                if (DepositPaid > 0)
                {
                    Console.WriteLine($"  ║  Avans plătit: {DepositPaid,5:F2} MDL (Tx:{TransactionId[..Math.Min(12,TransactionId.Length)]}...){"",-3}║");
                    Console.WriteLine($"  ║  Rest de plată:{BalanceDue,5:F2} MDL{"",-21}║");
                }
                Console.WriteLine($"  ║  {ConfirmationMsg,-44}║");
            }
            else
            {
                Console.WriteLine($"  ║  ❌ Rezervare eșuată: {ErrorMessage,-23}║");
            }
            Console.WriteLine($"  ╚══════════════════════════════════════════════╝");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. SUBSISTEME INTERNE (complexe, nu sunt expuse direct clientului)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Subsistem: gestionarea programărilor și verificarea disponibilității.
    /// Intern și complex – clientul nu îl accesează direct.
    /// </summary>
    internal class SchedulerService
    {
        private static int _bookingCounter = 1;

        // Simulăm un calendar cu sloturi ocupate
        private readonly HashSet<(string doctor, DateTime slot)> _occupiedSlots = new();

        public bool IsDoctorAvailable(string doctorName, DateTime dateTime)
        {
            Console.WriteLine($"    [Scheduler] Verificare disponibilitate: " +
                              $"{doctorName} la {dateTime:dd.MM.yyyy HH:mm}");
            // Simulăm că doctorul e disponibil (în producție: interogare BD)
            bool available = !_occupiedSlots.Contains((doctorName, dateTime));
            Console.WriteLine($"    [Scheduler] → {(available ? "Disponibil ✓" : "Ocupat ✗")}");
            return available;
        }

        public string ReserveSlot(string doctorName, string patientName, DateTime dateTime)
        {
            _occupiedSlots.Add((doctorName, dateTime));
            string bookingId = $"PRG-{_bookingCounter++:D5}";
            Console.WriteLine($"    [Scheduler] Slot rezervat → {bookingId}");
            return bookingId;
        }

        public void ReleaseSlot(string doctorName, DateTime dateTime)
        {
            _occupiedSlots.Remove((doctorName, dateTime));
            Console.WriteLine($"    [Scheduler] Slot eliberat: {doctorName} {dateTime:dd.MM.yyyy HH:mm}");
        }
    }

    /// <summary>
    /// Subsistem: validarea și înregistrarea datelor pacientului.
    /// </summary>
    internal class PatientValidationService
    {
        public (bool valid, string error) ValidatePatient(AppointmentRequest req)
        {
            Console.WriteLine($"    [PatientValidation] Validare date: {req.PatientName}");

            if (string.IsNullOrWhiteSpace(req.PatientName))
                return (false, "Numele pacientului este obligatoriu.");

            if (req.PatientAge < 1 || req.PatientAge > 120)
                return (false, $"Vârstă invalidă: {req.PatientAge}.");

            if (string.IsNullOrWhiteSpace(req.PatientPhone) &&
                string.IsNullOrWhiteSpace(req.PatientEmail))
                return (false, "Telefon sau email obligatoriu.");

            Console.WriteLine($"    [PatientValidation] → Date valide ✓");
            return (true, string.Empty);
        }

        public string RegisterOrUpdatePatient(AppointmentRequest req)
        {
            string patientId = $"PAC-{Math.Abs(req.PatientName.GetHashCode()) % 10000:D4}";
            Console.WriteLine($"    [PatientValidation] Pacient înregistrat/actualizat → {patientId}");
            return patientId;
        }
    }

    /// <summary>
    /// Subsistem: trimiterea notificărilor (email, SMS).
    /// </summary>
    internal class NotificationService
    {
        public void SendConfirmationEmail(string email, string patientName,
            string bookingId, DateTime appointmentTime, string serviceName)
        {
            Console.WriteLine($"    [Notification] Email trimis la {email}:");
            Console.WriteLine($"      Subiect: Confirmare programare #{bookingId}");
            Console.WriteLine($"      Corp: Stimate {patientName}, programarea dvs. " +
                              $"pentru {serviceName} este confirmată la " +
                              $"{appointmentTime:dd.MM.yyyy HH:mm}.");
        }

        public void SendSmsReminder(string phone, string patientName,
            DateTime appointmentTime)
        {
            Console.WriteLine($"    [Notification] SMS trimis la {phone}: " +
                              $"Reminder programare {appointmentTime:dd.MM.yyyy HH:mm}");
        }

        public void SendCancellationNotice(string email, string phone, string bookingId)
        {
            Console.WriteLine($"    [Notification] Notificare anulare #{bookingId} → {email} / {phone}");
        }
    }

    /// <summary>
    /// Subsistem: generarea și gestionarea facturilor.
    /// </summary>
    internal class InvoiceService
    {
        private static int _invoiceCounter = 1;

        public string GenerateInvoice(string patientName, string serviceName,
            decimal totalAmount, decimal depositPaid, string transactionId)
        {
            string invoiceId = $"FAC-{_invoiceCounter++:D6}";
            Console.WriteLine($"    [Invoice] Factură generată #{invoiceId}:");
            Console.WriteLine($"      Pacient: {patientName}");
            Console.WriteLine($"      Serviciu: {serviceName} → {totalAmount:F2} MDL");
            Console.WriteLine($"      Avans plătit: {depositPaid:F2} MDL (TxID: {transactionId})");
            Console.WriteLine($"      Rest de plătit: {totalAmount - depositPaid:F2} MDL");
            return invoiceId;
        }

        public void MarkAsPaid(string invoiceId, decimal amount, string transactionId)
        {
            Console.WriteLine($"    [Invoice] #{invoiceId} marcată ca plătită: " +
                              $"{amount:F2} MDL (Tx:{transactionId})");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. FAÇADE – interfața simplificată pentru client
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fațada principală a modulului de programări.
    ///
    /// Clientul (Program.cs, o altă clasă, un controller API) apelează
    /// DOAR metodele acestei clase. Nu cunoaște și nu instanțiază direct
    /// niciunul din subsistemele interne.
    ///
    /// Orchestrarea internă:
    ///   1. Validare date pacient
    ///   2. Verificare disponibilitate doctor
    ///   3. Rezervare slot în calendar
    ///   4. Procesare avans prin gateway-ul ales (Adapter)
    ///   5. Generare factură
    ///   6. Trimitere confirmare email + SMS
    ///   7. Înregistrare în audit log (Singleton din Lab3)
    ///   8. Returnare confirmare simplă către client
    /// </summary>
    public class AppointmentFacade
    {
        // Subsistemele interne – invizibile pentru client
        private readonly SchedulerService         _scheduler    = new();
        private readonly PatientValidationService _validation   = new();
        private readonly NotificationService      _notification = new();
        private readonly InvoiceService           _invoice      = new();
        private readonly AuditLog                 _audit        = AuditLog.Instance;

        // ── Metoda principală a Fațadei ───────────────────────────────

        /// <summary>
        /// Rezervă o programare completă. Clientul apelează DOAR această metodă.
        /// Intern, orchestrează 7+ subsisteme și returnează un rezultat simplu.
        /// </summary>
        public AppointmentConfirmation BookAppointment(AppointmentRequest request)
        {
            Console.WriteLine($"\n  [Façade] ▶ Inițiere rezervare pentru {request.PatientName}...");

            try
            {
                // ── Pasul 1: Validare date pacient ────────────────────
                Console.WriteLine("\n  [Façade] Pas 1/7 – Validare pacient");
                var (valid, error) = _validation.ValidatePatient(request);
                if (!valid)
                    return Fail(request, $"Validare eșuată: {error}");

                string patientId = _validation.RegisterOrUpdatePatient(request);

                // ── Pasul 2: Verificare disponibilitate ───────────────
                Console.WriteLine("\n  [Façade] Pas 2/7 – Verificare disponibilitate doctor");
                if (!_scheduler.IsDoctorAvailable(request.DoctorName, request.DesiredDateTime))
                    return Fail(request, $"{request.DoctorName} nu este disponibil la ora solicitată.");

                // ── Pasul 3: Rezervare slot ───────────────────────────
                Console.WriteLine("\n  [Façade] Pas 3/7 – Rezervare slot");
                string bookingId = _scheduler.ReserveSlot(
                    request.DoctorName, request.PatientName, request.DesiredDateTime);

                // ── Pasul 4: Procesare avans (30% din total) ──────────
                decimal totalPrice   = request.Service.Price;
                decimal depositAmount = Math.Round(totalPrice * 0.30m, 2);
                string  transactionId = string.Empty;

                if (request.PayDepositNow)
                {
                    Console.WriteLine("\n  [Façade] Pas 4/7 – Procesare avans (30%)");
                    IPaymentProcessor processor =
                        PaymentProcessorFactory.Create(request.PaymentMethod);

                    var paymentResult = processor.ProcessPayment(
                        request.PatientName,
                        depositAmount,
                        $"Avans programare #{bookingId} – {request.Service.Name}");

                    if (!paymentResult.Success)
                    {
                        // Eliberăm slotul dacă plata eșuează
                        _scheduler.ReleaseSlot(request.DoctorName, request.DesiredDateTime);
                        return Fail(request, $"Plată eșuată: {paymentResult.Message}");
                    }
                    transactionId = paymentResult.TransactionId;
                    Console.WriteLine($"    [Façade] Plată avans confirmată: {paymentResult}");
                }
                else
                {
                    Console.WriteLine("\n  [Façade] Pas 4/7 – Plată avans omisă (PayDepositNow=false)");
                    depositAmount = 0;
                }

                // ── Pasul 5: Generare factură ─────────────────────────
                Console.WriteLine("\n  [Façade] Pas 5/7 – Generare factură");
                string invoiceId = _invoice.GenerateInvoice(
                    request.PatientName, request.Service.Name,
                    totalPrice, depositAmount, transactionId);

                // ── Pasul 6: Trimitere confirmări ─────────────────────
                Console.WriteLine("\n  [Façade] Pas 6/7 – Trimitere notificări");
                if (!string.IsNullOrWhiteSpace(request.PatientEmail))
                    _notification.SendConfirmationEmail(
                        request.PatientEmail, request.PatientName,
                        bookingId, request.DesiredDateTime, request.Service.Name);

                if (!string.IsNullOrWhiteSpace(request.PatientPhone))
                    _notification.SendSmsReminder(
                        request.PatientPhone, request.PatientName, request.DesiredDateTime);

                // ── Pasul 7: Audit log ────────────────────────────────
                Console.WriteLine("\n  [Façade] Pas 7/7 – Înregistrare audit");
                _audit.Log("APPOINTMENT",
                    $"Programare #{bookingId} confirmată: {request.PatientName} → " +
                    $"{request.DoctorName}, {request.DesiredDateTime:dd.MM.yyyy HH:mm}, " +
                    $"{request.Service.Name}, avans {depositAmount:F2} MDL");

                // ── Returnare rezultat simplu ─────────────────────────
                Console.WriteLine("\n  [Façade] ✅ Rezervare finalizată cu succes.");
                return new AppointmentConfirmation
                {
                    Success         = true,
                    BookingId       = bookingId,
                    PatientName     = request.PatientName,
                    DoctorName      = request.DoctorName,
                    AppointmentTime = request.DesiredDateTime,
                    ServiceName     = request.Service.Name,
                    TotalPrice      = totalPrice,
                    DepositPaid     = depositAmount,
                    BalanceDue      = totalPrice - depositAmount,
                    TransactionId   = transactionId,
                    ConfirmationMsg = $"Confirmare trimisă la {request.PatientEmail}"
                };
            }
            catch (Exception ex)
            {
                _audit.Log("ERROR", $"Eroare rezervare {request.PatientName}: {ex.Message}");
                return Fail(request, $"Eroare internă: {ex.Message}");
            }
        }

        /// <summary>
        /// Anulează o programare existentă.
        /// Client-ul nu cunoaște pașii interni ai anulării.
        /// </summary>
        public bool CancelAppointment(string bookingId, string doctorName,
            DateTime appointmentTime, string patientEmail, string patientPhone)
        {
            Console.WriteLine($"\n  [Façade] ▶ Anulare programare #{bookingId}...");

            _scheduler.ReleaseSlot(doctorName, appointmentTime);
            _notification.SendCancellationNotice(patientEmail, patientPhone, bookingId);
            _audit.Log("APPOINTMENT", $"Programare #{bookingId} anulată.");

            Console.WriteLine($"  [Façade] ✅ Programare #{bookingId} anulată.");
            return true;
        }

        /// <summary>
        /// Procesează plata restului de sumă la sosirea pacientului.
        /// </summary>
        public PaymentResult ProcessBalancePayment(string bookingId, string patientName,
            decimal balanceAmount, PaymentProviderType paymentMethod)
        {
            Console.WriteLine($"\n  [Façade] ▶ Procesare rest plată #{bookingId}: {balanceAmount:F2} MDL");

            IPaymentProcessor processor = PaymentProcessorFactory.Create(paymentMethod);
            var result = processor.ProcessPayment(
                patientName, balanceAmount, $"Rest plată programare #{bookingId}");

            _audit.Log("PAYMENT",
                $"Rest plată #{bookingId}: {balanceAmount:F2} MDL via {processor.ProviderName} " +
                $"→ {(result.Success ? "OK" : "EȘUAT")}");

            return result;
        }

        // ── Helper ────────────────────────────────────────────────────

        private static AppointmentConfirmation Fail(AppointmentRequest req, string error)
        {
            Console.WriteLine($"  [Façade] ❌ {error}");
            AuditLog.Instance.Log("ERROR",
                $"Rezervare eșuată pentru {req.PatientName}: {error}");
            return new AppointmentConfirmation
            {
                Success      = false,
                PatientName  = req.PatientName,
                ErrorMessage = error
            };
        }
    }
}
