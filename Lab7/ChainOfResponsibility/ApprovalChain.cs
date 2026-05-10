// ═══════════════════════════════════════════════════════════════════════
//  CHAIN OF RESPONSIBILITY PATTERN
//  Domeniu: Pipeline de aprobare a tratamentelor costisitoare
//
//  Scenariu: Orice tratament care depășește un prag de cost necesită
//  aprobarea unui nivel ierarhic. Cererea urcă în lanț până când
//  cineva o poate aproba sau o respinge cu justificare.
//
//  Lanțul:
//   Asistentă  → aprobă  ≤ 500 MDL  (igienizări, consultații)
//   Medic      → aprobă  ≤ 2 000 MDL (obturații, extracții)
//   Șef clinică → aprobă ≤ 8 000 MDL (implanturi, ortodonție)
//   Director   → aprobă  orice sumă  (tratamente complexe, excepții)
//
//  Fiecare handler poate și să RESPINGĂ cererea (pacient blacklist,
//  documentație incompletă) — nu doar să o treacă mai departe.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab7.ChainOfResponsibility
{
    // ───────────────────────────────────────────────────────────────────
    // MODEL DE DATE
    // ───────────────────────────────────────────────────────────────────

    public enum TreatmentPriority { Routine, Urgent, Emergency }

    public class TreatmentApprovalRequest
    {
        private static int _counter = 1;
        public int      RequestId      { get; } = _counter++;
        public string   PatientName    { get; init; } = string.Empty;
        public string   PatientId      { get; init; } = string.Empty;
        public string   TreatmentName  { get; init; } = string.Empty;
        public decimal  EstimatedCost  { get; init; }
        public TreatmentPriority Priority { get; init; } = TreatmentPriority.Routine;
        public bool     HasInsurance   { get; init; }
        public bool     IsBlacklisted  { get; init; }
        public bool     DocumentsComplete { get; init; } = true;
        public string   RequestedBy    { get; init; } = string.Empty;

        public override string ToString() =>
            $"[Req#{RequestId}] {PatientName} | {TreatmentName} | " +
            $"{EstimatedCost:C} | {Priority}";
    }

    public class ApprovalResult
    {
        public bool    Approved      { get; init; }
        public string  ApprovedBy    { get; init; } = string.Empty;
        public string  Reason        { get; init; } = string.Empty;
        public int     HandlerLevel  { get; init; }
        public DateTime ProcessedAt  { get; init; } = DateTime.Now;

        public override string ToString() =>
            $"[{(Approved ? "APROBAT ✅" : "RESPINS ❌")}] " +
            $"de {ApprovedBy} (nivel {HandlerLevel}): {Reason}";
    }

    // ───────────────────────────────────────────────────────────────────
    // 1. HANDLERUL ABSTRACT – baza lanțului
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Handler abstract. Fiecare nivel din ierarhie îl extinde.
    /// SetNext() construiește lanțul prin fluent API.
    /// </summary>
    public abstract class ApprovalHandler
    {
        protected ApprovalHandler? _next;
        public abstract string HandlerName { get; }
        public abstract int    Level       { get; }

        /// <summary>Setează următorul handler — returnează next pentru înlănțuire.</summary>
        public ApprovalHandler SetNext(ApprovalHandler next)
        {
            _next = next;
            return next;
        }

        /// <summary>
        /// Metoda principală. Fiecare handler decide:
        ///  – procesează singur (returnează ApprovalResult), sau
        ///  – trimite mai departe la _next, sau
        ///  – respinge direct (fără a trimite mai departe).
        /// </summary>
        public ApprovalResult Handle(TreatmentApprovalRequest request)
        {
            // Verificări comune tuturor handlerilor
            if (request.IsBlacklisted)
                return Reject(request, "Pacient în lista neagră — acces blocat.");

            if (!request.DocumentsComplete)
                return Reject(request, "Documentație incompletă — completați dosarul.");

            return Process(request);
        }

        protected abstract ApprovalResult Process(TreatmentApprovalRequest request);

        protected ApprovalResult Approve(TreatmentApprovalRequest req, string reason)
        {
            Console.WriteLine($"  [{HandlerName}] ✅ APROBAT: {req} → {reason}");
            return new ApprovalResult
            {
                Approved     = true,
                ApprovedBy   = HandlerName,
                Reason       = reason,
                HandlerLevel = Level
            };
        }

        protected ApprovalResult Reject(TreatmentApprovalRequest req, string reason)
        {
            Console.WriteLine($"  [{HandlerName}] ❌ RESPINS: {req} → {reason}");
            return new ApprovalResult
            {
                Approved     = false,
                ApprovedBy   = HandlerName,
                Reason       = reason,
                HandlerLevel = Level
            };
        }

        protected ApprovalResult PassToNext(TreatmentApprovalRequest req)
        {
            if (_next == null)
                return Reject(req, $"Niciun handler superior disponibil pentru {req.EstimatedCost:C}.");

            Console.WriteLine($"  [{HandlerName}] ⏩ Transmis la {_next.HandlerName}...");
            return _next.Handle(req);
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. HANDLERI CONCREȚI
    // ───────────────────────────────────────────────────────────────────

    /// <summary>Nivel 1 – Asistentă medicală (≤ 500 MDL, rutină).</summary>
    public class NurseHandler : ApprovalHandler
    {
        public override string HandlerName => "Asistentă";
        public override int    Level       => 1;
        private const   decimal Limit      = 500m;

        protected override ApprovalResult Process(TreatmentApprovalRequest req)
        {
            // Asistenta aprobă doar tratamente rutiniere sub prag
            if (req.Priority == TreatmentPriority.Routine && req.EstimatedCost <= Limit)
                return Approve(req, $"Tratament rutinar sub {Limit:C} — aprobat automat.");

            return PassToNext(req);
        }
    }

    /// <summary>Nivel 2 – Medic stomatolog (≤ 2 000 MDL).</summary>
    public class DoctorHandler : ApprovalHandler
    {
        public override string HandlerName => "Medic Stomatolog";
        public override int    Level       => 2;
        private const   decimal Limit      = 2_000m;

        protected override ApprovalResult Process(TreatmentApprovalRequest req)
        {
            if (req.EstimatedCost <= Limit)
                return Approve(req, $"Tratament aprobat de medic (≤ {Limit:C}).");

            return PassToNext(req);
        }
    }

    /// <summary>Nivel 3 – Șef de clinică (≤ 8 000 MDL).</summary>
    public class ClinicHeadHandler : ApprovalHandler
    {
        public override string HandlerName => "Șef Clinică";
        public override int    Level       => 3;
        private const   decimal Limit      = 8_000m;

        protected override ApprovalResult Process(TreatmentApprovalRequest req)
        {
            if (req.EstimatedCost <= Limit)
                return Approve(req,
                    $"Tratament complex aprobat de șef clinică (≤ {Limit:C}).");

            // Dacă are asigurare, șeful poate aproba și mai mult
            if (req.HasInsurance && req.EstimatedCost <= Limit * 1.5m)
                return Approve(req,
                    $"Aprobat cu acoperire asigurare (≤ {Limit * 1.5m:C}).");

            return PassToNext(req);
        }
    }

    /// <summary>Nivel 4 – Director (aprobă orice, dar poate respinge cazuri extreme).</summary>
    public class DirectorHandler : ApprovalHandler
    {
        public override string HandlerName => "Director";
        public override int    Level       => 4;
        private const   decimal HardLimit  = 50_000m;

        protected override ApprovalResult Process(TreatmentApprovalRequest req)
        {
            if (req.EstimatedCost > HardLimit)
                return Reject(req,
                    $"Cost excesiv ({req.EstimatedCost:C}) — necesită comisie medicală.");

            if (req.Priority == TreatmentPriority.Emergency)
                return Approve(req, "Urgență medicală — aprobat prioritar de director.");

            return Approve(req, $"Aprobat de director. Sumă: {req.EstimatedCost:C}.");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. CHAIN BUILDER – construiește lanțul standard
    // ───────────────────────────────────────────────────────────────────

    public static class ApprovalChainFactory
    {
        /// <summary>Construiește lanțul complet: Asistentă → Medic → Șef → Director.</summary>
        public static ApprovalHandler BuildStandardChain()
        {
            var nurse  = new NurseHandler();
            var doctor = new DoctorHandler();
            var head   = new ClinicHeadHandler();
            var dir    = new DirectorHandler();

            // Fluent chaining
            nurse.SetNext(doctor).SetNext(head).SetNext(dir);
            return nurse;   // returnăm capătul lanțului
        }

        /// <summary>Lanț scurt pentru urgențe: sare direct la Director.</summary>
        public static ApprovalHandler BuildEmergencyChain()
        {
            var doctor = new DoctorHandler();
            var dir    = new DirectorHandler();
            doctor.SetNext(dir);
            return doctor;
        }
    }
}
