using System;
using System.Collections.Generic;
using DentalClinic.Models;

namespace DentalClinic.Observer
{
    // ════════════════════════════════════════════════════════════════════════════
    //  OBSERVER – Notificări Evenimente Programare
    //
    //  Problemă: când starea unei programări se schimbă (creare, confirmare,
    //  anulare), mai mulți actori trebuie notificați: pacientul, medicul,
    //  recepția, sistemul de raportare. Coupling direct → imposibil de extins.
    //
    //  Soluție: AppointmentScheduler = Subject; pacient/medic/recepție = Observer.
    //  Subjects-ul nu cunoaște concret observatorii – doar interfața IObserver.
    // ════════════════════════════════════════════════════════════════════════════

    // ─── Event args ───────────────────────────────────────────────────────────

    public class AppointmentEvent
    {
        public string      EventType   { get; }   // "Created","Confirmed","Cancelled","Rescheduled","Completed"
        public Appointment Appointment { get; }
        public DateTime    OccurredAt  { get; }
        public string      Message     { get; }

        public AppointmentEvent(string eventType, Appointment appointment, string message = "")
        {
            EventType   = eventType;
            Appointment = appointment;
            OccurredAt  = DateTime.Now;
            Message     = message;
        }
    }

    // ─── Observer interface ───────────────────────────────────────────────────

    public interface IAppointmentObserver
    {
        string ObserverName { get; }
        void OnAppointmentEvent(AppointmentEvent evt);
    }

    // ─── Subject interface ────────────────────────────────────────────────────

    public interface IAppointmentSubject
    {
        void Attach(IAppointmentObserver observer);
        void Detach(IAppointmentObserver observer);
        void NotifyObservers(AppointmentEvent evt);
    }

    // ─── Concrete Observers ───────────────────────────────────────────────────

    /// <summary>Trimite confirmare/reminder pacientului (simulat prin console).</summary>
    public class PatientNotificationObserver : IAppointmentObserver
    {
        public string ObserverName => "Notificare Pacient";

        public void OnAppointmentEvent(AppointmentEvent evt)
        {
            var appt = evt.Appointment;
            string msg = evt.EventType switch
            {
                "Created"     => $"📩 SMS → {appt.Patient.Phone}: Programarea dvs. din {appt.DateTime:dd.MM.yyyy HH:mm} la {appt.Doctor} a fost înregistrată.",
                "Confirmed"   => $"📩 SMS → {appt.Patient.Phone}: Programarea dvs. din {appt.DateTime:dd.MM.yyyy HH:mm} a fost CONFIRMATĂ.",
                "Cancelled"   => $"📩 SMS → {appt.Patient.Phone}: Programarea dvs. din {appt.DateTime:dd.MM.yyyy HH:mm} a fost ANULATĂ. {evt.Message}",
                "Rescheduled" => $"📩 SMS → {appt.Patient.Phone}: Programarea dvs. a fost REPROGRAMATĂ pentru {appt.DateTime:dd.MM.yyyy HH:mm}.",
                "Completed"   => $"📩 EMAIL → {appt.Patient.Email}: Vă mulțumim! Tratamentul din {appt.DateTime:dd.MM.yyyy} a fost finalizat.",
                _             => $"📩 → {appt.Patient.Name}: Actualizare programare [{evt.EventType}]"
            };
            Console.WriteLine($"    [PatientObserver]  {msg}");
        }
    }

    /// <summary>Actualizează calendarul medicului.</summary>
    public class DoctorCalendarObserver : IAppointmentObserver
    {
        public string ObserverName => "Calendar Medic";

        public void OnAppointmentEvent(AppointmentEvent evt)
        {
            var appt = evt.Appointment;
            string action = evt.EventType switch
            {
                "Created"     => $"➕ Adăugat în calendar: {appt.DateTime:dd.MM.yyyy HH:mm} – {appt.Patient.Name} ({appt.Service})",
                "Cancelled"   => $"🗑  Eliminat din calendar: {appt.DateTime:dd.MM.yyyy HH:mm} – {appt.Patient.Name}",
                "Rescheduled" => $"🔄 Actualizat în calendar: {appt.DateTime:dd.MM.yyyy HH:mm} – {appt.Patient.Name}",
                "Completed"   => $"✅ Marcat ca finalizat: {appt.Patient.Name} ({appt.DateTime:dd.MM.yyyy})",
                _             => $"ℹ  [{evt.EventType}] {appt.Patient.Name}"
            };
            Console.WriteLine($"    [DoctorObserver]   {appt.Doctor.Name}: {action}");
        }
    }

    /// <summary>Actualizează lista de așteptare a recepției.</summary>
    public class ReceptionDeskObserver : IAppointmentObserver
    {
        private readonly List<string> _waitingList = new();
        public string ObserverName => "Recepție";

        public void OnAppointmentEvent(AppointmentEvent evt)
        {
            var appt = evt.Appointment;
            switch (evt.EventType)
            {
                case "Created":
                    _waitingList.Add(appt.Id);
                    Console.WriteLine($"    [RecepțieObserver] 📋 Adăugat în registru: {appt} | Total astăzi: {_waitingList.Count}");
                    break;
                case "Cancelled":
                    _waitingList.Remove(appt.Id);
                    Console.WriteLine($"    [RecepțieObserver] 📋 Eliminat din registru: [{appt.Id}] {appt.Patient.Name}");
                    break;
                case "Confirmed":
                    Console.WriteLine($"    [RecepțieObserver] ✅ Confirmat: [{appt.Id}] {appt.Patient.Name} – {appt.DateTime:HH:mm}");
                    break;
            }
        }

        public int WaitingCount => _waitingList.Count;
    }

    /// <summary>Jurnalizează toate evenimentele pentru audit / raportare.</summary>
    public class AuditLogObserver : IAppointmentObserver
    {
        private readonly List<string> _log = new();
        public string ObserverName => "Audit Log";

        public void OnAppointmentEvent(AppointmentEvent evt)
        {
            string entry = $"[{evt.OccurredAt:HH:mm:ss}] {evt.EventType,-12} | Appt:{evt.Appointment.Id} | " +
                           $"Pacient:{evt.Appointment.Patient.Name} | Dr:{evt.Appointment.Doctor.Name}";
            _log.Add(entry);
            Console.WriteLine($"    [AuditObserver]    📝 {entry}");
        }

        public IReadOnlyList<string> GetLog() => _log.AsReadOnly();
        public int LogCount => _log.Count;
    }

    // ─── Subject: AppointmentScheduler ───────────────────────────────────────

    /// <summary>
    /// Sistemul central de programări. Menține lista de observatori și îi
    /// notifică automat la orice schimbare de stare.
    /// </summary>
    public class AppointmentScheduler : IAppointmentSubject
    {
        private readonly List<IAppointmentObserver> _observers = new();
        private readonly Dictionary<string, Appointment> _appointments = new();

        // ── Observer management ──────────────────────────────────────────────

        public void Attach(IAppointmentObserver observer)
        {
            _observers.Add(observer);
            Console.WriteLine($"  [Scheduler] Observer atașat: {observer.ObserverName}");
        }

        public void Detach(IAppointmentObserver observer)
        {
            _observers.Remove(observer);
            Console.WriteLine($"  [Scheduler] Observer detașat: {observer.ObserverName}");
        }

        public void NotifyObservers(AppointmentEvent evt)
        {
            foreach (var obs in _observers)
                obs.OnAppointmentEvent(evt);
        }

        // ── Business operations (fiecare declanșează notificări) ─────────────

        public void CreateAppointment(Appointment appt)
        {
            _appointments[appt.Id] = appt;
            appt.Status = AppointmentStatus.Scheduled;
            NotifyObservers(new AppointmentEvent("Created", appt));
        }

        public void ConfirmAppointment(string apptId)
        {
            if (!_appointments.TryGetValue(apptId, out var appt)) return;
            appt.Status = AppointmentStatus.Confirmed;
            NotifyObservers(new AppointmentEvent("Confirmed", appt));
        }

        public void CancelAppointment(string apptId, string reason = "")
        {
            if (!_appointments.TryGetValue(apptId, out var appt)) return;
            appt.Status = AppointmentStatus.Cancelled;
            NotifyObservers(new AppointmentEvent("Cancelled", appt, reason));
        }

        public void RescheduleAppointment(string apptId, DateTime newDateTime)
        {
            if (!_appointments.TryGetValue(apptId, out var appt)) return;
            appt.DateTime = newDateTime;
            appt.Status   = AppointmentStatus.Rescheduled;
            NotifyObservers(new AppointmentEvent("Rescheduled", appt));
        }

        public void CompleteAppointment(string apptId)
        {
            if (!_appointments.TryGetValue(apptId, out var appt)) return;
            appt.Status = AppointmentStatus.Completed;
            NotifyObservers(new AppointmentEvent("Completed", appt));
        }

        public Appointment GetAppointment(string id)
            => _appointments.TryGetValue(id, out var a) ? a : null;

        public IReadOnlyDictionary<string, Appointment> AllAppointments => _appointments;
    }
}
