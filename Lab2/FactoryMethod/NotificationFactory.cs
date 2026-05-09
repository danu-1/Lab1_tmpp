// ═══════════════════════════════════════════════════════════════════════
//  FACTORY METHOD PATTERN
//  Domeniu: Sistem de notificări pentru clinica stomatologică
//
//  Scenariu: Clinica trimite notificări pacienților în mai multe moduri
//  (Email, SMS, Push). Fiecare tip de notificare are propriul creator.
//  Codul client nu știe ce clasă concretă este instanțiată.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab2.FactoryMethod
{
    // ───────────────────────────────────────────────────────────────────
    // 1. PRODUSUL – interfața comună pentru toate notificările
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața produsului. Toate tipurile de notificare o implementează.
    /// </summary>
    public interface INotification
    {
        string Channel  { get; }           // "Email", "SMS", "Push"
        void   Send(string recipient, string subject, string body);
        string GetDeliveryReport();
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. PRODUSE CONCRETE
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Notificare prin Email.</summary>
    public class EmailNotification : INotification
    {
        private readonly List<string> _log = new();

        public string Channel => "Email";

        public void Send(string recipient, string subject, string body)
        {
            var msg = $"[EMAIL → {recipient}] Subiect: \"{subject}\" | {body}";
            Console.WriteLine(msg);
            _log.Add($"{DateTime.Now:HH:mm:ss} {msg}");
        }

        public string GetDeliveryReport() =>
            $"Email – {_log.Count} mesaje trimise.\n" +
            string.Join("\n", _log);
    }

    /// <summary>Notificare prin SMS.</summary>
    public class SmsNotification : INotification
    {
        private readonly List<string> _log = new();

        public string Channel => "SMS";

        public void Send(string recipient, string subject, string body)
        {
            // SMS: mesaj scurt, fără subiect separat
            var msg = $"[SMS → {recipient}] {subject}: {body[..Math.Min(body.Length, 60)]}...";
            Console.WriteLine(msg);
            _log.Add($"{DateTime.Now:HH:mm:ss} {msg}");
        }

        public string GetDeliveryReport() =>
            $"SMS – {_log.Count} mesaje trimise.\n" +
            string.Join("\n", _log);
    }

    /// <summary>Notificare Push (aplicație mobilă).</summary>
    public class PushNotification : INotification
    {
        private readonly List<string> _log = new();

        public string Channel => "Push";

        public void Send(string recipient, string subject, string body)
        {
            var msg = $"[PUSH → {recipient}] 🔔 {subject}";
            Console.WriteLine(msg);
            _log.Add($"{DateTime.Now:HH:mm:ss} {msg}");
        }

        public string GetDeliveryReport() =>
            $"Push – {_log.Count} notificări trimise.\n" +
            string.Join("\n", _log);
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. CREATORUL ABSTRACT – clasa cu Factory Method
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Clasa abstractă Creator.
    /// Definește Factory Method-ul <see cref="CreateNotification"/> și
    /// conține logica de business care FOLOSEȘTE produsul creat.
    /// Subclasele decid ce produs concret returnează.
    /// </summary>
    public abstract class NotificationCreator
    {
        // ── FACTORY METHOD ─────────────────────────────────────────────
        public abstract INotification CreateNotification();

        // ── Operație de business care folosește produsul ───────────────
        /// <summary>
        /// Trimite o notificare de confirmare programare.
        /// Nu știe și nu-i pasă ce tip concret de notificare se folosește.
        /// </summary>
        public void NotifyAppointmentConfirmed(
            string patientContact,
            string patientName,
            string doctorName,
            DateTime appointmentTime)
        {
            INotification notification = CreateNotification(); // apelul factory
            notification.Send(
                patientContact,
                "Confirmare programare",
                $"Stimate(ă) {patientName}, programarea la Dr. {doctorName} " +
                $"din {appointmentTime:dd.MM.yyyy} ora {appointmentTime:HH:mm} " +
                $"a fost CONFIRMATĂ.");
        }

        /// <summary>Trimite reminder cu 24h înainte de programare.</summary>
        public void NotifyAppointmentReminder(
            string patientContact,
            string patientName,
            DateTime appointmentTime)
        {
            INotification notification = CreateNotification();
            notification.Send(
                patientContact,
                "Reminder programare",
                $"{patientName}, mâine la {appointmentTime:HH:mm} aveți " +
                $"programare la clinica noastră. Vă așteptăm!");
        }

        /// <summary>Trimite notificare de plată restantă.</summary>
        public void NotifyPaymentOverdue(
            string patientContact,
            string patientName,
            decimal amount)
        {
            INotification notification = CreateNotification();
            notification.Send(
                patientContact,
                "Plată restantă",
                $"{patientName}, aveți o plată restantă de {amount:C}. " +
                $"Vă rugăm să o achitați cât mai curând.");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. CREATORI CONCREȚI – fiecare suprascrie Factory Method-ul
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Creator care produce notificări Email.</summary>
    public class EmailNotificationCreator : NotificationCreator
    {
        public override INotification CreateNotification() =>
            new EmailNotification();
    }

    /// <summary>Creator care produce notificări SMS.</summary>
    public class SmsNotificationCreator : NotificationCreator
    {
        public override INotification CreateNotification() =>
            new SmsNotification();
    }

    /// <summary>Creator care produce notificări Push.</summary>
    public class PushNotificationCreator : NotificationCreator
    {
        public override INotification CreateNotification() =>
            new PushNotification();
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. FACTORY METHOD CU PARAMETRU – variantă alternativă
    //    Un singur creator care decide tipul în funcție de preferința
    //    pacientului (stocată în profilul său).
    // ───────────────────────────────────────────────────────────────────

    public enum NotificationChannel { Email, Sms, Push }

    public class PatientNotificationCreator : NotificationCreator
    {
        private readonly NotificationChannel _preferredChannel;

        public PatientNotificationCreator(NotificationChannel preferredChannel)
        {
            _preferredChannel = preferredChannel;
        }

        public override INotification CreateNotification() =>
            _preferredChannel switch
            {
                NotificationChannel.Email => new EmailNotification(),
                NotificationChannel.Sms   => new SmsNotification(),
                NotificationChannel.Push  => new PushNotification(),
                _ => throw new NotSupportedException(
                    $"Canal necunoscut: {_preferredChannel}")
            };
    }
}
