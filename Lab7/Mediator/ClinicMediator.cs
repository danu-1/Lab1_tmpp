// ═══════════════════════════════════════════════════════════════════════
//  MEDIATOR PATTERN
//  Domeniu: Hub de coordonare a clinicii
//
//  Scenariu: Într-o clinică, mai multe departamente trebuie să
//  colaboreze, dar fără a se cunoaște direct între ele:
//    • Recepție creează programări
//    • Laborator primește cereri de analize
//    • Farmacie primește rețete
//    • Contabilitate primește facturile
//    • Sala de tratament primește pacienții
//
//  Fără Mediator: fiecare componentă ar ține referințe la toate
//  celelalte → rețea de dependențe O(N²).
//  Cu Mediator: fiecare componentă cunoaște DOAR mediatorul → O(N).
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab7.Mediator
{
    // ───────────────────────────────────────────────────────────────────
    // MODEL DE DATE
    // ───────────────────────────────────────────────────────────────────

    public class ClinicEvent
    {
        public string EventType   { get; init; } = string.Empty;
        public string SenderId    { get; init; } = string.Empty;
        public string PatientName { get; init; } = string.Empty;
        public string Details     { get; init; } = string.Empty;
        public object? Payload    { get; init; }
        public DateTime OccurredAt { get; init; } = DateTime.Now;
    }

    // ───────────────────────────────────────────────────────────────────
    // 1. INTERFEȚELE MEDIATOR și COLEG
    // ───────────────────────────────────────────────────────────────────

    public interface IClinicMediator
    {
        void Register(ClinicComponent component);
        void Notify(ClinicEvent evt);
        void Send(string targetId, ClinicEvent evt);
    }

    /// <summary>
    /// Colegul de bază. Fiecare departament îl extinde.
    /// Comunicarea se face EXCLUSIV prin mediator.
    /// </summary>
    public abstract class ClinicComponent
    {
        protected IClinicMediator _mediator;
        public abstract string ComponentId   { get; }
        public abstract string ComponentName { get; }
        public List<string> EventLog { get; } = new();

        protected ClinicComponent(IClinicMediator mediator)
        {
            _mediator = mediator;
            mediator.Register(this);
        }

        public abstract void ReceiveEvent(ClinicEvent evt);

        protected void Publish(ClinicEvent evt)
        {
            Console.WriteLine($"  [{ComponentName}] 📤 Publică: {evt.EventType} ({evt.Details})");
            _mediator.Notify(evt);
        }

        protected void SendTo(string targetId, ClinicEvent evt)
        {
            Console.WriteLine($"  [{ComponentName}] ➡️  Trimite la {targetId}: {evt.EventType}");
            _mediator.Send(targetId, evt);
        }

        protected void Log(string msg)
        {
            EventLog.Add($"[{DateTime.Now:HH:mm:ss}] {msg}");
            Console.WriteLine($"  [{ComponentName}] ℹ️  {msg}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. MEDIATORUL CONCRET
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hub-ul central al clinicii.
    /// Implementează logica de rutare a evenimentelor.
    /// Componentele nu se cunosc între ele — mediatorul știe tot.
    /// </summary>
    public class ClinicHub : IClinicMediator
    {
        private readonly Dictionary<string, ClinicComponent> _components = new();
        private readonly List<ClinicEvent> _eventHistory = new();

        public void Register(ClinicComponent comp)
        {
            _components[comp.ComponentId] = comp;
            Console.WriteLine($"  [ClinicHub] 🔗 Înregistrat: {comp.ComponentName}");
        }

        public void Notify(ClinicEvent evt)
        {
            _eventHistory.Add(evt);

            // Logica de rutare – mediatorul decide cine primește ce
            foreach (var (id, comp) in _components)
            {
                if (id != evt.SenderId)  // nu trimite înapoi la sender
                    comp.ReceiveEvent(evt);
            }
        }

        public void Send(string targetId, ClinicEvent evt)
        {
            _eventHistory.Add(evt);
            if (_components.TryGetValue(targetId, out var target))
                target.ReceiveEvent(evt);
            else
                Console.WriteLine($"  [ClinicHub] ⚠️ Target '{targetId}' inexistent.");
        }

        public IReadOnlyList<ClinicEvent> EventHistory => _eventHistory.AsReadOnly();
        public int ComponentCount => _components.Count;
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. COMPONENTE CONCRETE (Colegii)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Recepție – creează programări și coordonează sosirile.</summary>
    public class ReceptionDepartment : ClinicComponent
    {
        public override string ComponentId   => "RECEPTION";
        public override string ComponentName => "Recepție";
        public int AppointmentsCreated { get; private set; }

        public ReceptionDepartment(IClinicMediator mediator) : base(mediator) { }

        /// <summary>Creează o programare și notifică toate departamentele.</summary>
        public void CreateAppointment(string patient, string email, string phone,
            string doctor, DateTime at, string treatment)
        {
            AppointmentsCreated++;
            Log($"Programare creată: {patient} ({email}, {phone}) la Dr.{doctor}, {at:dd.MM HH:mm}, {treatment}");
            Publish(new ClinicEvent
            {
                EventType   = "APPOINTMENT_CREATED",
                SenderId    = ComponentId,
                PatientName = patient,
                Details     = $"Dr.{doctor} | {at:dd.MM.yyyy HH:mm} | {treatment}",
                Payload     = new { Doctor = doctor, At = at, Treatment = treatment, Email = email, Phone = phone }
            });
        }

        /// <summary>Pacient sosit – trimite direct la sala de tratament.</summary>
        public void PatientArrived(string patient, string doctor)
        {
            Log($"Pacient sosit: {patient}");
            SendTo("TREATMENT_ROOM", new ClinicEvent
            {
                EventType   = "PATIENT_ARRIVED",
                SenderId    = ComponentId,
                PatientName = patient,
                Details     = $"Pregătiți sala pentru Dr.{doctor}"
            });
        }

        public override void ReceiveEvent(ClinicEvent evt)
        {
            // Recepția e interesată de confirmări și anulări
            if (evt.EventType is "APPOINTMENT_CONFIRMED" or "APPOINTMENT_CANCELLED")
                Log($"Actualizare registru: {evt.EventType} – {evt.PatientName}");
        }
    }

    /// <summary>Sala de tratament – gestionează procedurile medicale.</summary>
    public class TreatmentRoomDepartment : ClinicComponent
    {
        public override string ComponentId   => "TREATMENT_ROOM";
        public override string ComponentName => "Sala Tratament";
        public int PatientsReceived { get; private set; }

        public TreatmentRoomDepartment(IClinicMediator mediator) : base(mediator) { }

        /// <summary>Tratament finalizat – declanșează facturare și farmacie.</summary>
        public void CompleteTreatment(string patient, string prescription, decimal cost)
        {
            Log($"Tratament finalizat: {patient} | Cost: {cost:C}");

            // Notifică toți: farmacie + contabilitate + recepție
            Publish(new ClinicEvent
            {
                EventType   = "TREATMENT_COMPLETED",
                SenderId    = ComponentId,
                PatientName = patient,
                Details     = $"Cost: {cost:C}",
                Payload     = new { Cost = cost, Prescription = prescription }
            });
        }

        public override void ReceiveEvent(ClinicEvent evt)
        {
            switch (evt.EventType)
            {
                case "PATIENT_ARRIVED":
                    PatientsReceived++;
                    Log($"Pacient primit: {evt.PatientName}. {evt.Details}");
                    break;
                case "LAB_RESULTS_READY":
                    Log($"Rezultate laborator primite pentru {evt.PatientName}: {evt.Details}");
                    break;
            }
        }
    }

    /// <summary>Laborator – procesează analize.</summary>
    public class LaboratoryDepartment : ClinicComponent
    {
        public override string ComponentId   => "LAB";
        public override string ComponentName => "Laborator";
        public int AnalysesProcessed { get; private set; }

        public LaboratoryDepartment(IClinicMediator mediator) : base(mediator) { }

        public void SendResults(string patient, string results)
        {
            AnalysesProcessed++;
            Log($"Rezultate gata pentru {patient}: {results}");
            Publish(new ClinicEvent
            {
                EventType   = "LAB_RESULTS_READY",
                SenderId    = ComponentId,
                PatientName = patient,
                Details     = results
            });
        }

        public override void ReceiveEvent(ClinicEvent evt)
        {
            if (evt.EventType == "APPOINTMENT_CREATED")
                Log($"Programare nouă înregistrată: {evt.PatientName} – pregătire posibilă analiză.");
        }
    }

    /// <summary>Farmacie – procesează rețete.</summary>
    public class PharmacyDepartment : ClinicComponent
    {
        public override string ComponentId   => "PHARMACY";
        public override string ComponentName => "Farmacie";
        public List<string> PrescriptionsProcessed { get; } = new();

        public PharmacyDepartment(IClinicMediator mediator) : base(mediator) { }

        public override void ReceiveEvent(ClinicEvent evt)
        {
            if (evt.EventType != "TREATMENT_COMPLETED") return;

            dynamic? payload = evt.Payload;
            string rx = payload?.Prescription ?? "—";
            if (rx != "—" && rx != "")
            {
                PrescriptionsProcessed.Add($"{evt.PatientName}: {rx}");
                Log($"Rețetă procesată pentru {evt.PatientName}: {rx}");
            }
        }
    }

    /// <summary>Contabilitate – generează facturi.</summary>
    public class AccountingDepartment : ClinicComponent
    {
        public override string ComponentId   => "ACCOUNTING";
        public override string ComponentName => "Contabilitate";
        public decimal TotalRevenue { get; private set; }
        public int     InvoiceCount { get; private set; }

        public AccountingDepartment(IClinicMediator mediator) : base(mediator) { }

        public override void ReceiveEvent(ClinicEvent evt)
        {
            if (evt.EventType != "TREATMENT_COMPLETED") return;

            dynamic? payload = evt.Payload;
            decimal cost = (decimal)(payload?.Cost ?? 0m);
            TotalRevenue += cost;
            InvoiceCount++;
            Log($"Factură #{InvoiceCount} generată: {evt.PatientName} – {cost:C} " +
                $"(Total: {TotalRevenue:C})");
        }
    }
}
