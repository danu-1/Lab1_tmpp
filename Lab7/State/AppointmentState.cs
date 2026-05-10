// ═══════════════════════════════════════════════════════════════════════
//  STATE PATTERN
//  Domeniu: Ciclul de viață al unei programări stomatologice
//
//  Scenariu: O programare trece printr-o mașină de stări bine definită.
//  Comportamentul ei depinde de starea curentă — aceleași metode
//  (confirm, cancel, complete) au efecte diferite în stări diferite.
//
//  Stări și tranziții valide:
//
//  [Scheduled] ──confirm()──→ [Confirmed]
//  [Scheduled] ──cancel()───→ [Cancelled]
//  [Confirmed] ──start()────→ [InProgress]
//  [Confirmed] ──cancel()───→ [Cancelled]
//  [InProgress]──complete()─→ [Completed]
//  [InProgress]──pause()────→ [OnHold]
//  [OnHold]    ──resume()───→ [InProgress]
//  [OnHold]    ──cancel()───→ [Cancelled]
//  [Completed] ──(terminal) – nicio tranziție
//  [Cancelled] ──(terminal) – nicio tranziție
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab7.State
{
    // ───────────────────────────────────────────────────────────────────
    // 1. INTERFAȚA STĂRII
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fiecare stare implementează aceleași acțiuni.
    /// Unele sunt valide în starea curentă, altele aruncă excepție.
    /// </summary>
    public interface IAppointmentState
    {
        string StateName { get; }
        void Confirm(AppointmentContext ctx);
        void Start(AppointmentContext ctx);
        void Pause(AppointmentContext ctx, string reason);
        void Resume(AppointmentContext ctx);
        void Complete(AppointmentContext ctx, string notes);
        void Cancel(AppointmentContext ctx, string reason);
        string GetAvailableActions();
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. CONTEXT – obiectul a cărui stare se schimbă
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Contextul menține starea curentă și delegă toate acțiunile la ea.
    /// Stările concrete apelează SetState() pentru a schimba starea.
    /// </summary>
    public class AppointmentContext
    {
        private IAppointmentState _state;

        public int      Id            { get; }
        public string   PatientName   { get; }
        public string   DoctorName    { get; }
        public string   Treatment     { get; }
        public DateTime ScheduledAt   { get; }

        // Istoricul tranzițiilor
        public List<(DateTime At, string From, string To, string Note)> History { get; } = new();

        public AppointmentContext(
            int id, string patient, string doctor,
            string treatment, DateTime scheduledAt)
        {
            Id          = id;
            PatientName = patient;
            DoctorName  = doctor;
            Treatment   = treatment;
            ScheduledAt = scheduledAt;
            _state      = new ScheduledState();
            Log("—", _state.StateName, "Programare creată");
        }

        public string CurrentStateName => _state.StateName;

        // ── Delegare la starea curentă ─────────────────────────────────
        public void Confirm()                           => _state.Confirm(this);
        public void Start()                             => _state.Start(this);
        public void Pause(string reason)                => _state.Pause(this, reason);
        public void Resume()                            => _state.Resume(this);
        public void Complete(string notes = "")         => _state.Complete(this, notes);
        public void Cancel(string reason)               => _state.Cancel(this, reason);
        public string GetAvailableActions()             => _state.GetAvailableActions();

        /// <summary>Schimbă starea — apelat de stările concrete.</summary>
        internal void SetState(IAppointmentState newState, string note = "")
        {
            string oldName = _state.StateName;
            _state = newState;
            Log(oldName, newState.StateName, note);
            Console.WriteLine($"  [Appt#{Id}] {oldName} ──→ {newState.StateName}" +
                (note != "" ? $" ({note})" : ""));
        }

        private void Log(string from, string to, string note)
            => History.Add((DateTime.Now, from, to, note));

        public void PrintHistory()
        {
            Console.WriteLine($"\n  Istoricul programării #{Id} – {PatientName}:");
            foreach (var (at, from, to, note) in History)
                Console.WriteLine($"    [{at:HH:mm:ss}] {from,-12} → {to,-12} | {note}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. STĂRI CONCRETE
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Starea inițială – programare creată, neconfirmată.</summary>
    public class ScheduledState : IAppointmentState
    {
        public string StateName => "Scheduled";

        public void Confirm(AppointmentContext ctx)
            => ctx.SetState(new ConfirmedState(), "Pacient a confirmat prezența");

        public void Cancel(AppointmentContext ctx, string reason)
            => ctx.SetState(new CancelledState(), $"Anulat: {reason}");

        public void Start(AppointmentContext ctx)
            => Invalid("Start", "trebuie confirmată mai întâi");

        public void Pause(AppointmentContext ctx, string reason)
            => Invalid("Pause", "nu a început încă");

        public void Resume(AppointmentContext ctx)
            => Invalid("Resume", "nu a început încă");

        public void Complete(AppointmentContext ctx, string notes)
            => Invalid("Complete", "nu a început încă");

        public string GetAvailableActions() => "Confirm() | Cancel()";

        private static void Invalid(string action, string why)
            => throw new InvalidOperationException(
                $"Acțiunea '{action}' nu este permisă în starea Scheduled ({why}).");
    }

    /// <summary>Programare confirmată – pacientul a anunțat că vine.</summary>
    public class ConfirmedState : IAppointmentState
    {
        public string StateName => "Confirmed";

        public void Start(AppointmentContext ctx)
            => ctx.SetState(new InProgressState(), "Pacient sosit, tratament început");

        public void Cancel(AppointmentContext ctx, string reason)
            => ctx.SetState(new CancelledState(), $"Anulat după confirmare: {reason}");

        public void Confirm(AppointmentContext ctx)
            => throw new InvalidOperationException("Deja confirmată.");

        public void Pause(AppointmentContext ctx, string reason)
            => Invalid("Pause");

        public void Resume(AppointmentContext ctx)
            => Invalid("Resume");

        public void Complete(AppointmentContext ctx, string notes)
            => Invalid("Complete");

        public string GetAvailableActions() => "Start() | Cancel()";

        private static void Invalid(string a)
            => throw new InvalidOperationException($"'{a}' nu e permis în Confirmed.");
    }

    /// <summary>Tratament în desfășurare.</summary>
    public class InProgressState : IAppointmentState
    {
        public string StateName => "InProgress";

        public void Complete(AppointmentContext ctx, string notes)
            => ctx.SetState(new CompletedState(),
                $"Finalizat. {(notes != "" ? notes : "Fără complicații.")}");

        public void Pause(AppointmentContext ctx, string reason)
            => ctx.SetState(new OnHoldState(), $"Pauză: {reason}");

        public void Confirm(AppointmentContext ctx)
            => throw new InvalidOperationException("Deja în desfășurare.");

        public void Start(AppointmentContext ctx)
            => throw new InvalidOperationException("Deja started.");

        public void Resume(AppointmentContext ctx)
            => throw new InvalidOperationException("Nu e în pauză.");

        public void Cancel(AppointmentContext ctx, string reason)
            => throw new InvalidOperationException(
                "Nu se poate anula un tratament în desfășurare. Folosiți Pause.");

        public string GetAvailableActions() => "Complete() | Pause()";
    }

    /// <summary>Tratament suspendat temporar (anestezic nefuncțional, urgență etc.).</summary>
    public class OnHoldState : IAppointmentState
    {
        public string StateName => "OnHold";

        public void Resume(AppointmentContext ctx)
            => ctx.SetState(new InProgressState(), "Tratament reluat");

        public void Cancel(AppointmentContext ctx, string reason)
            => ctx.SetState(new CancelledState(), $"Anulat din pauză: {reason}");

        public void Confirm(AppointmentContext ctx) => Invalid("Confirm");
        public void Start(AppointmentContext ctx)   => Invalid("Start");
        public void Pause(AppointmentContext ctx, string r) => Invalid("Pause");
        public void Complete(AppointmentContext ctx, string n)
            => throw new InvalidOperationException("Reluați tratamentul înainte de a-l finaliza.");

        public string GetAvailableActions() => "Resume() | Cancel()";

        private static void Invalid(string a)
            => throw new InvalidOperationException($"'{a}' nu e permis în OnHold.");
    }

    /// <summary>Stare terminală pozitivă.</summary>
    public class CompletedState : IAppointmentState
    {
        public string StateName => "Completed";

        public void Confirm(AppointmentContext ctx)  => Terminal();
        public void Start(AppointmentContext ctx)    => Terminal();
        public void Pause(AppointmentContext ctx, string r) => Terminal();
        public void Resume(AppointmentContext ctx)   => Terminal();
        public void Complete(AppointmentContext ctx, string n) => Terminal();
        public void Cancel(AppointmentContext ctx, string r)
            => throw new InvalidOperationException("Tratament deja finalizat — nu poate fi anulat.");

        public string GetAvailableActions() => "(stare terminală — nicio acțiune)";

        private static void Terminal()
            => throw new InvalidOperationException("Programare finalizată — stare imuabilă.");
    }

    /// <summary>Stare terminală negativă.</summary>
    public class CancelledState : IAppointmentState
    {
        public string StateName => "Cancelled";

        public void Confirm(AppointmentContext ctx)  => Terminal();
        public void Start(AppointmentContext ctx)    => Terminal();
        public void Pause(AppointmentContext ctx, string r) => Terminal();
        public void Resume(AppointmentContext ctx)   => Terminal();
        public void Complete(AppointmentContext ctx, string n) => Terminal();
        public void Cancel(AppointmentContext ctx, string r)   => Terminal();

        public string GetAvailableActions() => "(stare terminală — nicio acțiune)";

        private static void Terminal()
            => throw new InvalidOperationException("Programare anulată — stare imuabilă.");
    }
}
