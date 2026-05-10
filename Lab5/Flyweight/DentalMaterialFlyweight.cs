// ═══════════════════════════════════════════════════════════════════════
//  FLYWEIGHT PATTERN
//  Domeniu: Gestionarea înregistrărilor de tratament la scară mare
//
//  Scenariu: Clinica procesează zeci de mii de înregistrări de tratament.
//  Fiecare înregistrare conține date despre materialul folosit și tipul
//  de dinte tratat. Aceste date sunt IDENTICE pentru mii de înregistrări
//  (ex: toți pacienții cu obturație pe molar folosesc același material).
//
//  Fără Flyweight: 10.000 înregistrări × date repetitive = RAM irosit
//  Cu Flyweight:   date intrinseci partajate, doar contextul e unic
//
//  Stare INTRINSECĂ (partajată în Flyweight):
//    – tipul materialului, proprietăți, cost/unitate, producător
//    – tipul dintelui, localizare anatomică, număr rădăcini
//
//  Stare EXTRINSECĂ (unică per înregistrare, pasată la operație):
//    – ID pacient, data tratamentului, cantitatea folosită, medicul
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab5.Flyweight
{
    // ───────────────────────────────────────────────────────────────────
    // 1. FLYWEIGHT – date intrinseci despre materialul dentar
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Flyweight: conține DOAR starea intrinsecă (imuabilă, partajabilă).
    /// O singură instanță per tip de material – indiferent de câte
    /// tratamente folosesc acel material.
    /// </summary>
    public class DentalMaterialFlyweight
    {
        // Stare intrinsecă – imuabilă, partajată
        public string MaterialCode    { get; }
        public string MaterialName    { get; }
        public string Category        { get; }   // Compozit, Amalgam, GIC, etc.
        public string Manufacturer    { get; }
        public decimal CostPerUnit    { get; }   // cost per 0.1g
        public string Shade           { get; }   // A1, A2, B2, etc.
        public bool   IsRadioOpaque   { get; }
        public string StorageRequirements { get; }

        public DentalMaterialFlyweight(
            string code, string name, string category,
            string manufacturer, decimal costPerUnit,
            string shade, bool isRadioOpaque, string storage)
        {
            MaterialCode        = code;
            MaterialName        = name;
            Category            = category;
            Manufacturer        = manufacturer;
            CostPerUnit         = costPerUnit;
            Shade               = shade;
            IsRadioOpaque       = isRadioOpaque;
            StorageRequirements = storage;
        }

        /// <summary>
        /// Operația Flyweight primește starea EXTRINSECĂ ca parametru.
        /// Nu o stochează – o folosește doar în această operație.
        /// </summary>
        public void ApplyToTreatment(
            string patientId, string doctorId,
            DateTime treatmentDate, double quantityGrams)
        {
            decimal totalCost = (decimal)(quantityGrams * 10) * CostPerUnit;
            Console.WriteLine(
                $"  Material: {MaterialName} ({Shade}) | " +
                $"Pacient: {patientId} | Dr: {doctorId} | " +
                $"{treatmentDate:dd.MM.yy} | {quantityGrams:F2}g | Cost: {totalCost:C}");
        }

        public decimal CalculateCost(double quantityGrams)
            => (decimal)(quantityGrams * 10) * CostPerUnit;

        public override string ToString()
            => $"[{MaterialCode}] {MaterialName} {Shade} ({Category}) – {CostPerUnit:C}/unitate";
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. FLYWEIGHT – date intrinseci despre tipul de dinte
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Flyweight pentru tipul de dinte. Datele anatomice sunt identice
    /// pentru toți pacienții care au același tip de dinte.
    /// </summary>
    public class ToothTypeFlyweight
    {
        public string ToothCode        { get; }   // FDI: 11,12,...,48
        public string ToothName        { get; }
        public string Position         { get; }   // Superior/Inferior
        public string Side             { get; }   // Drept/Stâng
        public int    NumberOfRoots    { get; }
        public int    NumberOfCanals   { get; }
        public bool   IsDeciduous      { get; }   // dinte de lapte
        public string AnatomicNotes    { get; }

        public ToothTypeFlyweight(
            string code, string name, string position, string side,
            int roots, int canals, bool isDeciduous, string notes)
        {
            ToothCode     = code;
            ToothName     = name;
            Position      = position;
            Side          = side;
            NumberOfRoots = roots;
            NumberOfCanals = canals;
            IsDeciduous   = isDeciduous;
            AnatomicNotes = notes;
        }

        public string GetAnatomicSummary()
            => $"{ToothName} ({ToothCode}) – {Position} {Side} | " +
               $"{NumberOfRoots} rădăcini, {NumberOfCanals} canale" +
               (IsDeciduous ? " [lapte]" : "");
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. FLYWEIGHT FACTORY – gestionează pool-urile de instanțe
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Factory centralizat. Returnează instanța existentă dacă există,
    /// sau creează una nouă și o stochează în pool.
    /// NICIODATĂ nu se creează două instanțe cu același cod.
    /// </summary>
    public class DentalFlyweightFactory
    {
        private readonly Dictionary<string, DentalMaterialFlyweight> _materials = new();
        private readonly Dictionary<string, ToothTypeFlyweight>      _toothTypes = new();

        // Statistici pentru demonstrarea economiei de memorie
        private int _materialRequests  = 0;
        private int _toothTypeRequests = 0;

        // ── Material Flyweights ────────────────────────────────────────
        public DentalMaterialFlyweight GetMaterial(string code)
        {
            _materialRequests++;
            if (!_materials.ContainsKey(code))
                throw new KeyNotFoundException($"Material '{code}' inexistent în fabrică.");
            return _materials[code];
        }

        public void RegisterMaterial(DentalMaterialFlyweight material)
            => _materials[material.MaterialCode] = material;

        // ── ToothType Flyweights ───────────────────────────────────────
        public ToothTypeFlyweight GetToothType(string code)
        {
            _toothTypeRequests++;
            if (!_toothTypes.ContainsKey(code))
                throw new KeyNotFoundException($"Tip dinte '{code}' inexistent.");
            return _toothTypes[code];
        }

        public void RegisterToothType(ToothTypeFlyweight toothType)
            => _toothTypes[toothType.ToothCode] = toothType;

        // ── Statistici de memorie ──────────────────────────────────────
        public int  UniqueMatCount     => _materials.Count;
        public int  UniqueToothCount   => _toothTypes.Count;
        public int  TotalMatRequests   => _materialRequests;
        public int  TotalToothRequests => _toothTypeRequests;

        /// <summary>
        /// Estimare economie RAM.
        /// Un obiect complet = ~500 bytes; un flyweight = ~150 bytes shared.
        /// </summary>
        public void PrintMemoryReport(int totalRecords)
        {
            int withoutFlyweight = totalRecords * 500;    // bytes
            int withFlyweight    = UniqueMatCount * 150
                                 + UniqueToothCount * 120
                                 + totalRecords * 80;     // stare extrinsecă per record

            Console.WriteLine($"\n  ── Raport Flyweight ────────────────────────────");
            Console.WriteLine($"  Instanțe unice materiale : {UniqueMatCount}");
            Console.WriteLine($"  Instanțe unice dinti     : {UniqueToothCount}");
            Console.WriteLine($"  Total cereri (materiale) : {TotalMatRequests}");
            Console.WriteLine($"  Total cereri (dinti)     : {TotalToothRequests}");
            Console.WriteLine($"  Înregistrări simulate    : {totalRecords:N0}");
            Console.WriteLine($"  RAM estimat FĂRĂ Flyweight: ~{withoutFlyweight / 1024:N0} KB");
            Console.WriteLine($"  RAM estimat CU  Flyweight : ~{withFlyweight / 1024:N0} KB");
            Console.WriteLine($"  Economie estimată         : " +
                $"~{(withoutFlyweight - withFlyweight) / 1024:N0} KB " +
                $"({(1.0 - (double)withFlyweight / withoutFlyweight) * 100:F1}%)");
            Console.WriteLine($"  ────────────────────────────────────────────────");
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. CONTEXT – starea extrinsecă per înregistrare (rămâne unică)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Înregistrare de tratament. Stochează REFERINȚE la flyweights
    /// și propriile date extrinseci (unice per record).
    /// Nu duplică niciun byte din datele flyweight.
    /// </summary>
    public class TreatmentRecord
    {
        // Stare extrinsecă – unică per înregistrare
        public string   RecordId      { get; }
        public string   PatientId     { get; }
        public string   DoctorId      { get; }
        public DateTime TreatmentDate { get; }
        public double   MaterialQty   { get; }   // grame
        public string   ClinicalNotes { get; }

        // Referințe la flyweights (nu copii!)
        private readonly DentalMaterialFlyweight _material;
        private readonly ToothTypeFlyweight      _toothType;

        public TreatmentRecord(
            string recordId, string patientId, string doctorId,
            DateTime date, double materialQty, string notes,
            DentalMaterialFlyweight material, ToothTypeFlyweight toothType)
        {
            RecordId      = recordId;
            PatientId     = patientId;
            DoctorId      = doctorId;
            TreatmentDate = date;
            MaterialQty   = materialQty;
            ClinicalNotes = notes;
            _material     = material;
            _toothType    = toothType;
        }

        public decimal GetTreatmentCost()
            => _material.CalculateCost(MaterialQty);

        public void Display()
        {
            Console.WriteLine($"  Record {RecordId}: {_toothType.GetAnatomicSummary()}");
            _material.ApplyToTreatment(PatientId, DoctorId, TreatmentDate, MaterialQty);
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. FACTORY pentru date standard
    // ───────────────────────────────────────────────────────────────────

    public static class FlyweightDataSeeder
    {
        public static DentalFlyweightFactory CreateSeededFactory()
        {
            var factory = new DentalFlyweightFactory();

            // Materiale dentare
            factory.RegisterMaterial(new DentalMaterialFlyweight(
                "M001","Filtek Supreme Ultra","Compozit nano",
                "3M ESPE", 12.50m, "A2", true, "Ferit de lumină, 4-25°C"));
            factory.RegisterMaterial(new DentalMaterialFlyweight(
                "M002","Tetric EvoCeram","Compozit nano-hibrid",
                "Ivoclar Vivadent", 14.80m, "B2", true, "Ferit de lumină"));
            factory.RegisterMaterial(new DentalMaterialFlyweight(
                "M003","Fuji IX GP","GIC convențional",
                "GC America", 8.20m, "A3", false, "Uscat, 10-25°C"));
            factory.RegisterMaterial(new DentalMaterialFlyweight(
                "M004","AH Plus","Sealer endodontic",
                "Dentsply", 6.50m, "N/A", true, "2-8°C"));
            factory.RegisterMaterial(new DentalMaterialFlyweight(
                "M005","Biodentine","Material bioceramic",
                "Septodont", 18.00m, "White", false, "15-25°C"));

            // Tipuri de dinte (notație FDI)
            factory.RegisterToothType(new ToothTypeFlyweight(
                "11","Incisiv central superior drept","Superior","Drept",
                1,1,false,"Cel mai lung dinte frontal"));
            factory.RegisterToothType(new ToothTypeFlyweight(
                "16","Primul molar superior drept","Superior","Drept",
                3,3,false,"3 rădăcini: MV, DV, P"));
            factory.RegisterToothType(new ToothTypeFlyweight(
                "36","Primul molar inferior drept","Inferior","Drept",
                2,3,false,"2 rădăcini: M(2 canale), D(1 canal)"));
            factory.RegisterToothType(new ToothTypeFlyweight(
                "46","Primul molar inferior stâng","Inferior","Stâng",
                2,3,false,"Cel mai frecvent tratat endodontic"));
            factory.RegisterToothType(new ToothTypeFlyweight(
                "55","Al doilea molar temporar drept","Superior","Drept",
                3,3,true,"Dinte de lapte – predecessor M1 perm."));

            return factory;
        }
    }
}
