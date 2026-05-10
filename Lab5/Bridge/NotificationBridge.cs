// ═══════════════════════════════════════════════════════════════════════
//  BRIDGE PATTERN
//  Domeniu: Sistem de reminder-uri pentru programări
//
//  Problema fără Bridge: pentru fiecare combinație (tip mesaj × canal)
//  ai nevoie de o clasă separată:
//    AppointmentReminderEmail, AppointmentReminderSms,
//    PaymentReminderEmail, PaymentReminderSms,
//    FollowUpReminderEmail, FollowUpReminderSms ...
//  → N tipuri × M canale = N×M clase = explozie combinatorică
//
//  Cu Bridge: abstractizarea (tipul de mesaj) și implementarea
//  (canalul de trimitere) evoluează INDEPENDENT.
//    – Adaugi un tip nou de mesaj → 1 clasă
//    – Adaugi un canal nou → 1 implementare
//    – Total mereu: N + M clase (nu N × M)
//
//  ABSTRACTIZARE  : ce tip de notificare (Appointment, Payment, FollowUp)
//  IMPLEMENTARE   : prin ce canal (Email, SMS, Push, WhatsApp)
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab5.Bridge
{
    // ───────────────────────────────────────────────────────────────────
    // 1. IMPLEMENTARE (Implementor) – interfața canalului de trimitere
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața implementorului. Definește operațiile primitive
    /// de trimitere. Abstractizarea nu cunoaște detaliile tehnice.
    /// </summary>
    public interface IMessageChannel
    {
        string ChannelName { get; }
        void SendMessage(string recipient, string subject, string body, MessagePriority priority);
        bool IsAvailable(string recipient);
        int  GetMaxLength();   // limita de caractere a canalului
    }

    public enum MessagePriority { Low, Normal, High, Urgent }

    // ───────────────────────────────────────────────────────────────────
    // 2. IMPLEMENTĂRI CONCRETE ale canalelor
    // ───────────────────────────────────────────────────────────────────

    public class EmailChannel : IMessageChannel
    {
        public string ChannelName => "Email";
        public int    GetMaxLength() => 10_000;

        public void SendMessage(string recipient, string subject, string body, MessagePriority priority)
        {
            string urgency = priority >= MessagePriority.High ? "[URGENT] " : "";
            Console.WriteLine($"    📧 EMAIL → {recipient}");
            Console.WriteLine($"       Subiect: {urgency}{subject}");
            Console.WriteLine($"       Corp (primele 80 ch): {body[..Math.Min(body.Length,80)]}...");
        }

        public bool IsAvailable(string recipient) => recipient.Contains('@');
    }

    public class SmsChannel : IMessageChannel
    {
        public string ChannelName => "SMS";
        public int    GetMaxLength() => 160;

        public void SendMessage(string recipient, string subject, string body, MessagePriority priority)
        {
            // SMS: text scurt, fără subiect separat
            string truncated = body.Length > 140 ? body[..137] + "..." : body;
            Console.WriteLine($"    📱 SMS → {recipient}: {truncated}");
        }

        public bool IsAvailable(string recipient) => recipient.StartsWith('+') || recipient.StartsWith('0');
    }

    public class PushChannel : IMessageChannel
    {
        public string ChannelName => "Push Notification";
        public int    GetMaxLength() => 256;

        public void SendMessage(string recipient, string subject, string body, MessagePriority priority)
        {
            string icon = priority == MessagePriority.Urgent ? "🚨" : "🔔";
            Console.WriteLine($"    {icon} PUSH → {recipient}");
            Console.WriteLine($"       Titlu: {subject}");
        }

        public bool IsAvailable(string recipient) => recipient.StartsWith("token_");
    }

    public class WhatsAppChannel : IMessageChannel
    {
        public string ChannelName => "WhatsApp";
        public int    GetMaxLength() => 4_096;

        public void SendMessage(string recipient, string subject, string body, MessagePriority priority)
        {
            Console.WriteLine($"    💬 WHATSAPP → {recipient}");
            Console.WriteLine($"       *{subject}*\n       {body[..Math.Min(body.Length,100)]}");
        }

        public bool IsAvailable(string recipient) => recipient.StartsWith('+');
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. ABSTRACTIZARE – tipul de notificare (conține bridge-ul)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Abstractizarea de bază. Conține referința la implementor (bridge).
    /// Subclasele definesc tipul de mesaj; implementorul definește canalul.
    /// </summary>
    public abstract class ClinicNotification
    {
        // Bridge-ul: referința la implementor (injectată prin constructor)
        protected IMessageChannel _channel;

        protected ClinicNotification(IMessageChannel channel)
            => _channel = channel;

        /// <summary>Înlocuiește canalul la runtime – fără a schimba tipul notificării.</summary>
        public void SetChannel(IMessageChannel channel) => _channel = channel;

        public string ChannelName => _channel.ChannelName;

        /// <summary>Trimite notificarea. Fiecare subclasă compune mesajul diferit.</summary>
        public abstract void Send(NotificationContext ctx);

        /// <summary>Trunchiază corpul la limita canalului dacă e necesar.</summary>
        protected string FitToChannel(string body)
        {
            int max = _channel.GetMaxLength();
            return body.Length > max ? body[..(max - 3)] + "..." : body;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. CONTEXT – datele necesare pentru compunerea mesajului
    // ───────────────────────────────────────────────────────────────────

    public record NotificationContext
    {
        public string   PatientName    { get; init; } = string.Empty;
        public string   PatientContact { get; init; } = string.Empty;
        public string   DoctorName     { get; init; } = string.Empty;
        public DateTime AppointmentDate { get; init; }
        public string   TreatmentType  { get; init; } = string.Empty;
        public decimal  Amount         { get; init; }
        public string   ClinicPhone    { get; init; } = "+373 22 123 456";
        public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. ABSTRACTIZĂRI RAFINATE (Refined Abstraction)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Reminder pentru o programare viitoare.</summary>
    public class AppointmentReminderNotification : ClinicNotification
    {
        private readonly int _hoursBeforeAppointment;

        public AppointmentReminderNotification(IMessageChannel channel, int hoursBefore = 24)
            : base(channel) => _hoursBeforeAppointment = hoursBefore;

        public override void Send(NotificationContext ctx)
        {
            if (!_channel.IsAvailable(ctx.PatientContact))
            {
                Console.WriteLine($"    ⚠️  Contact '{ctx.PatientContact}' incompatibil cu {_channel.ChannelName}");
                return;
            }

            string subject = $"Reminder programare – {ctx.AppointmentDate:dd.MM.yyyy}";
            string body    = FitToChannel(
                $"Stimate(ă) {ctx.PatientName}, " +
                $"vă reamintim că aveți o programare " +
                $"MÂINE {ctx.AppointmentDate:dd.MM.yyyy} la ora {ctx.AppointmentDate:HH:mm} " +
                $"la Dr. {ctx.DoctorName} pentru {ctx.TreatmentType}. " +
                $"Confirmare/anulare: {ctx.ClinicPhone}");

            _channel.SendMessage(ctx.PatientContact, subject, body, ctx.Priority);
        }
    }

    /// <summary>Confirmare după înregistrarea programării.</summary>
    public class AppointmentConfirmationNotification : ClinicNotification
    {
        public AppointmentConfirmationNotification(IMessageChannel channel) : base(channel) { }

        public override void Send(NotificationContext ctx)
        {
            if (!_channel.IsAvailable(ctx.PatientContact))
            {
                Console.WriteLine($"    ⚠️  Contact incompatibil cu {_channel.ChannelName}");
                return;
            }

            string subject = "Programare confirmată – DentaCare Clinic";
            string body    = FitToChannel(
                $"Programarea dvs., {ctx.PatientName}, a fost înregistrată cu succes. " +
                $"Data: {ctx.AppointmentDate:dd.MM.yyyy HH:mm}, " +
                $"Medic: Dr. {ctx.DoctorName}, Serviciu: {ctx.TreatmentType}. " +
                $"Vă așteptăm! {ctx.ClinicPhone}");

            _channel.SendMessage(ctx.PatientContact, subject, body, MessagePriority.Normal);
        }
    }

    /// <summary>Notificare plată restantă.</summary>
    public class PaymentOverdueNotification : ClinicNotification
    {
        public PaymentOverdueNotification(IMessageChannel channel) : base(channel) { }

        public override void Send(NotificationContext ctx)
        {
            if (!_channel.IsAvailable(ctx.PatientContact))
            {
                Console.WriteLine($"    ⚠️  Contact incompatibil cu {_channel.ChannelName}");
                return;
            }

            string subject = $"Plată restantă: {ctx.Amount:C}";
            string body    = FitToChannel(
                $"{ctx.PatientName}, aveți o sumă neachitată " +
                $"de {ctx.Amount:C} pentru tratamentul din " +
                $"{ctx.AppointmentDate:dd.MM.yyyy}. " +
                $"Vă rugăm să achitați în cel mai scurt timp. " +
                $"Informații: {ctx.ClinicPhone}");

            _channel.SendMessage(ctx.PatientContact, subject, body, MessagePriority.High);
        }
    }

    /// <summary>Notificare post-tratament (follow-up).</summary>
    public class FollowUpNotification : ClinicNotification
    {
        private readonly int _daysAfterTreatment;

        public FollowUpNotification(IMessageChannel channel, int daysAfter = 3)
            : base(channel) => _daysAfterTreatment = daysAfter;

        public override void Send(NotificationContext ctx)
        {
            if (!_channel.IsAvailable(ctx.PatientContact))
            {
                Console.WriteLine($"    ⚠️  Contact incompatibil cu {_channel.ChannelName}");
                return;
            }

            string subject = "Follow-up tratament – Cum vă simțiți?";
            string body    = FitToChannel(
                $"Bună ziua, {ctx.PatientName}! " +
                $"Au trecut {_daysAfterTreatment} zile de la tratamentul dumneavoastră " +
                $"({ctx.TreatmentType}). " +
                $"Sperăm că vă simțiți bine. Dacă aveți nelămuriri, " +
                $"contactați-ne: {ctx.ClinicPhone}");

            _channel.SendMessage(ctx.PatientContact, subject, body, MessagePriority.Low);
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 6. ABSTRACTIZARE RAFINATĂ – notificare multi-canal (Extended Bridge)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Abstractizare rafinată: trimite pe TOATE canalele disponibile.
    /// Demonstrează că abstractizarea poate gestiona mai mulți implementori.
    /// </summary>
    public class MultiChannelNotification : ClinicNotification
    {
        private readonly List<IMessageChannel> _channels;
        private readonly ClinicNotification    _notificationType;

        public MultiChannelNotification(
            ClinicNotification notificationType,
            params IMessageChannel[] channels)
            : base(channels[0])
        {
            _notificationType = notificationType;
            _channels         = channels.ToList();
        }

        public override void Send(NotificationContext ctx)
        {
            Console.WriteLine($"    [Multi-canal: {string.Join(", ", _channels.Select(c => c.ChannelName))}]");
            foreach (var channel in _channels)
            {
                _notificationType.SetChannel(channel);
                _notificationType.Send(ctx);
            }
        }
    }
}
