// ═══════════════════════════════════════════════════════════════════════
//  BUILDER PATTERN
//  Domeniu: Construirea unui Plan de Tratament personalizat
//
//  Scenariu: Medicul construiește pas-cu-pas un plan de tratament complex
//  pentru un pacient. Planul include proceduri, medicamente, programări,
//  instrucțiuni post-tratament și detalii de facturare.
//  Builder separă construcția de reprezentarea finală și permite
//  crearea de variante diferite ale aceluiași obiect complex.
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab3.Builder
{
    // ───────────────────────────────────────────────────────────────────
    // 1. VALUE OBJECTS folosite de produs
    // ───────────────────────────────────────────────────────────────────

    public class Procedure
    {
        public string  Name         { get; init; } = string.Empty;
        public int     DurationMins { get; init; }
        public decimal Cost         { get; init; }
        public string  ToothArea    { get; init; } = string.Empty;

        public override string ToString() =>
            $"• {Name} ({ToothArea}) – {DurationMins} min – {Cost:C}";
    }

    public class Medication
    {
        public string Name       { get; init; } = string.Empty;
        public string Dosage     { get; init; } = string.Empty;
        public int    DaysCourse { get; init; }

        public override string ToString() =>
            $"• {Name} {Dosage} × {DaysCourse} zile";
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. PRODUSUL – obiectul complex construit de Builder
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Produsul final. Constructor intern – se poate crea
    /// doar prin TreatmentPlanBuilder.
    /// </summary>
    public class TreatmentPlan
    {
        internal TreatmentPlan() { }

        public string       PlanId             { get; internal set; } = string.Empty;
        public string       PatientName        { get; internal set; } = string.Empty;
        public string       DoctorName         { get; internal set; } = string.Empty;
        public DateTime     CreatedAt          { get; internal set; }
        public string       Diagnosis          { get; internal set; } = string.Empty;
        public string       Urgency            { get; internal set; } = "Normală";
        public bool         RequiresAnesthesia { get; internal set; }
        public bool         InsuranceCovered   { get; internal set; }
        public string       InsuranceProvider  { get; internal set; } = string.Empty;
        public decimal      Discount           { get; internal set; }
        public string       Notes              { get; internal set; } = string.Empty;

        public List<Procedure> Procedures    { get; internal set; } = new();
        public List<Medication> Medications  { get; internal set; } = new();
        public List<DateTime>  Appointments  { get; internal set; } = new();
        public List<string>    AfterCareNotes { get; internal set; } = new();

        // Proprietăți calculate
        public decimal TotalCost =>
            Math.Round(Procedures.Sum(p => p.Cost) * (1 - Discount), 2);

        public int TotalDurationMins =>
            Procedures.Sum(p => p.DurationMins);

        public void Print()
        {
            Console.WriteLine($"\n{"",1}{"═",1}{"".PadRight(54,'═')}");
            Console.WriteLine($"  PLAN DE TRATAMENT #{PlanId}");
            Console.WriteLine($"{"",1}{"═",1}{"".PadRight(54,'═')}");
            Console.WriteLine($"  Pacient    : {PatientName}");
            Console.WriteLine($"  Medic      : {DoctorName}");
            Console.WriteLine($"  Data       : {CreatedAt:dd.MM.yyyy HH:mm}");
            Console.WriteLine($"  Diagnostic : {Diagnosis}");
            Console.WriteLine($"  Urgență    : {Urgency}");
            Console.WriteLine($"  Anestezie  : {(RequiresAnesthesia ? "Da" : "Nu")}");

            Console.WriteLine($"\n  PROCEDURI ({Procedures.Count}):");
            foreach (var p in Procedures) Console.WriteLine($"  {p}");

            if (Medications.Count > 0)
            {
                Console.WriteLine($"\n  MEDICAMENTE ({Medications.Count}):");
                foreach (var m in Medications) Console.WriteLine($"  {m}");
            }

            if (Appointments.Count > 0)
            {
                Console.WriteLine($"\n  PROGRAMĂRI ({Appointments.Count}):");
                foreach (var a in Appointments)
                    Console.WriteLine($"  • {a:dd.MM.yyyy HH:mm}");
            }

            if (AfterCareNotes.Count > 0)
            {
                Console.WriteLine("\n  INSTRUCȚIUNI POST-TRATAMENT:");
                foreach (var n in AfterCareNotes)
                    Console.WriteLine($"  • {n}");
            }

            Console.WriteLine("\n  FACTURARE:");
            if (InsuranceCovered)
                Console.WriteLine($"  Asigurare : {InsuranceProvider}");
            if (Discount > 0)
                Console.WriteLine($"  Reducere  : {Discount * 100:F0}%");
            Console.WriteLine($"  TOTAL     : {TotalCost:C}  (~{TotalDurationMins} min)");

            if (!string.IsNullOrWhiteSpace(Notes))
                Console.WriteLine($"\n  NOTE: {Notes}");

            Console.WriteLine($"{"",1}{"═",1}{"".PadRight(54,'═')}");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. INTERFAȚA BUILDER
    // ───────────────────────────────────────────────────────────────────

    public interface ITreatmentPlanBuilder
    {
        ITreatmentPlanBuilder ForPatient(string patientName);
        ITreatmentPlanBuilder ByDoctor(string doctorName);
        ITreatmentPlanBuilder WithDiagnosis(string diagnosis);
        ITreatmentPlanBuilder WithUrgency(string urgency);
        ITreatmentPlanBuilder AddProcedure(string name, string toothArea, int durationMins, decimal cost);
        ITreatmentPlanBuilder AddMedication(string name, string dosage, int daysCourse);
        ITreatmentPlanBuilder ScheduleAppointment(DateTime dateTime);
        ITreatmentPlanBuilder AddAfterCareNote(string note);
        ITreatmentPlanBuilder RequiresAnesthesia(bool required = true);
        ITreatmentPlanBuilder WithInsurance(string provider);
        ITreatmentPlanBuilder WithDiscount(decimal discountRate);
        ITreatmentPlanBuilder WithNotes(string notes);
        TreatmentPlan         Build();
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. BUILDER CONCRET cu Fluent API
    // ───────────────────────────────────────────────────────────────────

    public class TreatmentPlanBuilder : ITreatmentPlanBuilder
    {
        private static int    _counter = 1;
        private TreatmentPlan _plan    = new();

        public TreatmentPlanBuilder() => Reset();

        private void Reset()
        {
            _plan = new TreatmentPlan
            {
                PlanId    = $"TP-{_counter++:D4}",
                CreatedAt = DateTime.Now
            };
        }

        public ITreatmentPlanBuilder ForPatient(string name)
            { _plan.PatientName = name; return this; }

        public ITreatmentPlanBuilder ByDoctor(string name)
            { _plan.DoctorName = name; return this; }

        public ITreatmentPlanBuilder WithDiagnosis(string d)
            { _plan.Diagnosis = d; return this; }

        public ITreatmentPlanBuilder WithUrgency(string u)
            { _plan.Urgency = u; return this; }

        public ITreatmentPlanBuilder AddProcedure(
            string name, string area, int mins, decimal cost)
        {
            _plan.Procedures.Add(new Procedure
            {
                Name = name, ToothArea = area,
                DurationMins = mins, Cost = cost
            });
            return this;
        }

        public ITreatmentPlanBuilder AddMedication(
            string name, string dosage, int days)
        {
            _plan.Medications.Add(new Medication
            { Name = name, Dosage = dosage, DaysCourse = days });
            return this;
        }

        public ITreatmentPlanBuilder ScheduleAppointment(DateTime dt)
            { _plan.Appointments.Add(dt); return this; }

        public ITreatmentPlanBuilder AddAfterCareNote(string note)
            { _plan.AfterCareNotes.Add(note); return this; }

        public ITreatmentPlanBuilder RequiresAnesthesia(bool req = true)
            { _plan.RequiresAnesthesia = req; return this; }

        public ITreatmentPlanBuilder WithInsurance(string provider)
        {
            _plan.InsuranceCovered  = true;
            _plan.InsuranceProvider = provider;
            return this;
        }

        public ITreatmentPlanBuilder WithDiscount(decimal rate)
        {
            if (rate < 0 || rate > 1)
                throw new ArgumentException("Reducerea trebuie să fie între 0 și 1.");
            _plan.Discount = rate;
            return this;
        }

        public ITreatmentPlanBuilder WithNotes(string notes)
            { _plan.Notes = notes; return this; }

        /// <summary>
        /// Finalizează planul și resetează builder-ul pentru reutilizare.
        /// </summary>
        public TreatmentPlan Build()
        {
            if (string.IsNullOrWhiteSpace(_plan.PatientName))
                throw new InvalidOperationException("Pacientul este obligatoriu.");
            if (string.IsNullOrWhiteSpace(_plan.DoctorName))
                throw new InvalidOperationException("Medicul este obligatoriu.");
            if (_plan.Procedures.Count == 0)
                throw new InvalidOperationException("Minimum o procedură necesară.");

            var result = _plan;
            Reset();
            return result;
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. DIRECTOR – rețete de construire predefinite
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Director-ul cunoaște "rețetele" standard.
    /// Clientul nu mai trebuie să cunoască ordinea pașilor.
    /// </summary>
    public class TreatmentPlanDirector
    {
        private readonly ITreatmentPlanBuilder _builder;

        public TreatmentPlanDirector(ITreatmentPlanBuilder builder)
            => _builder = builder;

        /// <summary>Plan de urgență – extracție + antibiotic.</summary>
        public TreatmentPlan BuildEmergencyExtractionPlan(
            string patient, string doctor) =>
            _builder
                .ForPatient(patient).ByDoctor(doctor)
                .WithDiagnosis("Dinte irecuperabil – indicație de extracție")
                .WithUrgency("URGENTĂ")
                .RequiresAnesthesia()
                .AddProcedure("Anestezie locală",      "Maxilar",  10,  80m)
                .AddProcedure("Extracție simplă",      "Molar 3",  20, 250m)
                .AddProcedure("Sutură post-extracție", "Molar 3",  15, 120m)
                .AddMedication("Amoxicilină", "500mg × 3/zi", 7)
                .AddMedication("Ibuprofen",   "400mg × 3/zi", 3)
                .ScheduleAppointment(DateTime.Now.AddDays(1).Date.AddHours(9))
                .AddAfterCareNote("Nu consumați alimente tari 48h.")
                .AddAfterCareNote("Clătire cu apă sărată după 24h.")
                .AddAfterCareNote("Reveniți dacă durerea persistă > 3 zile.")
                .Build();

        /// <summary>Plan igienizare anuală.</summary>
        public TreatmentPlan BuildAnnualHygienePlan(
            string patient, string doctor) =>
            _builder
                .ForPatient(patient).ByDoctor(doctor)
                .WithDiagnosis("Control rutină – igienizare profesională")
                .AddProcedure("Detartraj ultrasonic", "Arcada sup.", 30, 200m)
                .AddProcedure("Detartraj ultrasonic", "Arcada inf.", 30, 200m)
                .AddProcedure("Airflow",              "Complet",     20, 150m)
                .AddProcedure("Periaj profesional",   "Complet",     15,  80m)
                .AddProcedure("Fluorurare",           "Complet",     10,  60m)
                .ScheduleAppointment(DateTime.Now.AddDays(7).Date.AddHours(10))
                .AddAfterCareNote("Nu consumați alimente colorate 2h.")
                .AddAfterCareNote("Periaj 2× pe zi + ață dentară.")
                .Build();

        /// <summary>Plan ortodontic cu reducere opțională pentru studenți.</summary>
        public TreatmentPlan BuildOrthodonticPlan(
            string patient, string doctor, bool isStudent)
        {
            var builder = _builder
                .ForPatient(patient).ByDoctor(doctor)
                .WithDiagnosis("Malocluziune cl.II – aparate fixe")
                .WithUrgency("Planificată")
                .AddProcedure("Consultație ortodontică", "Complet",     30,  300m)
                .AddProcedure("Radiografie panoramică",  "Complet",     10,  200m)
                .AddProcedure("Montare aparate fixe",    "Arcada sup.", 90, 2500m)
                .AddProcedure("Montare aparate fixe",    "Arcada inf.", 90, 2500m)
                .AddMedication("Gel fluor", "aplicare zilnică", 30)
                .ScheduleAppointment(DateTime.Now.AddDays(5).Date.AddHours(9))
                .ScheduleAppointment(DateTime.Now.AddMonths(1).Date.AddHours(9))
                .ScheduleAppointment(DateTime.Now.AddMonths(2).Date.AddHours(9))
                .AddAfterCareNote("Evitați alimente lipicioase și tari.")
                .AddAfterCareNote("Periaj cu perie interdentară.")
                .WithNotes("Durata estimată tratament: 18-24 luni.");

            if (isStudent) builder = builder.WithDiscount(0.15m);
            return builder.Build();
        }
    }
}
