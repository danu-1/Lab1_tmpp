// ═══════════════════════════════════════════════════════════════════════
//  COMPOSITE PATTERN
//  Domeniu: Ierarhia serviciilor stomatologice (servicii & pachete)
//
//  Scenariu: Clinica oferă servicii individuale (Consultație, Obturație)
//  și pachete compuse din mai multe servicii (Pachet Implant, Pachet
//  Igienizare Completă). Un pachet poate conține atât servicii individuale
//  cât și alte pachete (ex: Pachet Implant include Pachet Chirurgical +
//  coroana protetică). Clientul (sistemul de facturare) tratează uniform
//  atât un serviciu simplu cât și un pachet complex.
//
//  Participanți:
//  – Component : IDentalServiceComponent  (interfața comună)
//  – Leaf      : DentalService            (serviciu individual)
//  – Composite : ServicePackage           (pachet de servicii)
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab4.Composite
{
    // ───────────────────────────────────────────────────────────────────
    // 1. COMPONENT – interfața comună pentru Leaf și Composite
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața uniformă: atât un serviciu individual cât și un pachet
    /// întreg sunt tratate identic de codul client.
    /// </summary>
    public interface IDentalServiceComponent
    {
        string  Name        { get; }
        string  Category    { get; }
        decimal Price       { get; }   // prețul final (cu discount aplicat dacă există)
        int     DurationMins { get; }  // durata totală în minute
        bool    IsComposite { get; }   // false = serviciu simplu, true = pachet

        /// <summary>Afișează detalii (cu indentare pentru ierarhii).</summary>
        void Print(int indent = 0);

        /// <summary>Returnează toate serviciile individuale (frunzele) din ierarhie.</summary>
        IEnumerable<DentalService> GetAllLeafServices();
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. LEAF – DentalService (serviciu individual, fără copii)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serviciu stomatologic individual: consultație, obturație, extracție etc.
    /// Nu poate conține alte componente (nod frunză în arbore).
    /// </summary>
    public class DentalService : IDentalServiceComponent
    {
        public string  Name         { get; init; } = string.Empty;
        public string  Category     { get; init; } = string.Empty;
        public string  ToothArea    { get; set; }  = string.Empty;
        public decimal BasePrice    { get; init; }
        public int     DurationMins { get; init; }
        public string  DoctorSpecialty { get; init; } = string.Empty;
        public bool    RequiresAnesthesia { get; init; }
        public string  Notes        { get; set; }  = string.Empty;

        // IDentalServiceComponent
        public decimal Price      => BasePrice;   // serviciu simplu: preț direct
        public bool    IsComposite => false;

        public void Print(int indent = 0)
        {
            string pad = new(' ', indent * 3);
            Console.WriteLine($"{pad}• {Name,-38} [{Category,-18}] " +
                              $"{DurationMins,3} min   {Price,8:F2} MDL");
            if (!string.IsNullOrWhiteSpace(Notes))
                Console.WriteLine($"{pad}  ↳ {Notes}");
        }

        public IEnumerable<DentalService> GetAllLeafServices()
        {
            yield return this;
        }

        public override string ToString() => $"{Name} ({Price:F2} MDL)";
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. COMPOSITE – ServicePackage (pachet de componente)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pachet stomatologic: conține servicii individuale și/sau alte pachete.
    /// Calculează automat prețul și durata totală din copii.
    /// Poate aplica un discount pe întregul pachet.
    /// </summary>
    public class ServicePackage : IDentalServiceComponent
    {
        private readonly List<IDentalServiceComponent> _children = new();

        public string  Name        { get; init; } = string.Empty;
        public string  Category    { get; init; } = string.Empty;
        public string  Description { get; set; }  = string.Empty;
        public decimal Discount    { get; init; } = 0m; // ex: 0.10 = 10% reducere pe pachet

        public bool IsComposite => true;

        // Prețul = suma copiilor cu discount aplicat pe pachet
        public decimal Price =>
            Math.Round(_children.Sum(c => c.Price) * (1 - Discount), 2);

        // Durata = suma duratelor copiilor
        public int DurationMins =>
            _children.Sum(c => c.DurationMins);

        public int ComponentCount => _children.Count;

        // ── Gestionarea copiilor ───────────────────────────────────────

        /// <summary>Adaugă un serviciu sau sub-pachet în acest pachet.</summary>
        public ServicePackage Add(IDentalServiceComponent component)
        {
            _children.Add(component);
            return this; // fluent API
        }

        /// <summary>Elimină o componentă din pachet.</summary>
        public bool Remove(IDentalServiceComponent component) =>
            _children.Remove(component);

        public IReadOnlyList<IDentalServiceComponent> Children => _children.AsReadOnly();

        // ── IDentalServiceComponent ────────────────────────────────────

        public void Print(int indent = 0)
        {
            string pad    = new(' ', indent * 3);
            string disc   = Discount > 0 ? $" [reducere {Discount * 100:F0}%]" : "";
            Console.WriteLine($"{pad}▶ {Name}{disc}");
            if (!string.IsNullOrWhiteSpace(Description))
                Console.WriteLine($"{pad}  {Description}");
            Console.WriteLine($"{pad}  {"Componente:",-20} {_children.Count}");
            Console.WriteLine($"{pad}  {"Durată totală:",-20} {DurationMins} min");
            Console.WriteLine($"{pad}  {"Preț total:",-20} {Price:F2} MDL" +
                              (Discount > 0
                                  ? $"  (față de {_children.Sum(c=>c.Price):F2} MDL fără reducere)"
                                  : ""));

            foreach (var child in _children)
                child.Print(indent + 1);
        }

        public IEnumerable<DentalService> GetAllLeafServices() =>
            _children.SelectMany(c => c.GetAllLeafServices());

        public override string ToString() =>
            $"[Pachet] {Name} – {ComponentCount} componente – {Price:F2} MDL";
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. CATALOG DE SERVICII – factory de servicii și pachete predefinite
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Catalogul clinicii: definește serviciile individuale și pachetele standard.
    /// Demonstrează cum Composite permite construirea de ierarhii flexibile.
    /// </summary>
    public static class ServiceCatalog
    {
        // ── Servicii individuale (Leaf) ────────────────────────────────

        public static DentalService Consultatie() => new()
        {
            Name = "Consultație inițială", Category = "Diagnostic",
            BasePrice = 200m, DurationMins = 20,
            DoctorSpecialty = "Stomatologie generală"
        };

        public static DentalService Radiografie() => new()
        {
            Name = "Radiografie dentară", Category = "Diagnostic",
            BasePrice = 120m, DurationMins = 10,
            Notes = "Radiografie periapicală digitală"
        };

        public static DentalService AnestezieLocala() => new()
        {
            Name = "Anestezie locală", Category = "Anestezie",
            BasePrice = 80m, DurationMins = 10,
            RequiresAnesthesia = true
        };

        public static DentalService ObturationComposite() => new()
        {
            Name = "Obturație compozit", Category = "Restaurare",
            BasePrice = 320m, DurationMins = 35,
            DoctorSpecialty = "Stomatologie restauratoare",
            Notes = "Compozit fotopolimerizabil, clasa II"
        };

        public static DentalService Detartraj() => new()
        {
            Name = "Detartraj ultrasonic", Category = "Igienizare",
            BasePrice = 200m, DurationMins = 30,
            DoctorSpecialty = "Igienistă dentară"
        };

        public static DentalService Airflow() => new()
        {
            Name = "Airflow (sandblasting)", Category = "Igienizare",
            BasePrice = 150m, DurationMins = 20
        };

        public static DentalService PeriajProfesional() => new()
        {
            Name = "Periaj profesional", Category = "Igienizare",
            BasePrice = 80m, DurationMins = 15
        };

        public static DentalService Fluorurare() => new()
        {
            Name = "Fluorurare", Category = "Profilaxie",
            BasePrice = 60m, DurationMins = 10
        };

        public static DentalService ExtractieSimpa() => new()
        {
            Name = "Extracție simplă", Category = "Chirurgie orală",
            BasePrice = 250m, DurationMins = 20,
            RequiresAnesthesia = true,
            DoctorSpecialty = "Chirurg oral"
        };

        public static DentalService Sutura() => new()
        {
            Name = "Sutură post-extracție", Category = "Chirurgie orală",
            BasePrice = 120m, DurationMins = 15
        };

        public static DentalService ImplantFixare() => new()
        {
            Name = "Fixare implant (titan)", Category = "Implantologie",
            BasePrice = 4500m, DurationMins = 60,
            RequiresAnesthesia = true,
            DoctorSpecialty = "Implantolog",
            Notes = "Implant Nobel Biocare Ø3.75×11.5 mm"
        };

        public static DentalService CoroanaZirconiu() => new()
        {
            Name = "Coroană zirconiu", Category = "Protetică",
            BasePrice = 3200m, DurationMins = 45,
            DoctorSpecialty = "Proteticist"
        };

        public static DentalService BontImplant() => new()
        {
            Name = "Bont implant standard", Category = "Implantologie",
            BasePrice = 800m, DurationMins = 20
        };

        // ── Pachete predefinite (Composite) ───────────────────────────

        /// <summary>Pachet igienizare completă cu discount 5%.</summary>
        public static ServicePackage PachetIgienizareCompleta() =>
            new ServicePackage
            {
                Name        = "Pachet Igienizare Completă",
                Category    = "Igienizare",
                Description = "Detartraj + Airflow + Periaj profesional + Fluorurare",
                Discount    = 0.05m
            }
            .Add(Detartraj())
            .Add(Airflow())
            .Add(PeriajProfesional())
            .Add(Fluorurare());

        /// <summary>Pachet consultație + radiografie (diagnostic complet).</summary>
        public static ServicePackage PachetDiagnosticComplet() =>
            new ServicePackage
            {
                Name        = "Pachet Diagnostic Complet",
                Category    = "Diagnostic",
                Description = "Consultație + radiografie periapicală"
            }
            .Add(Consultatie())
            .Add(Radiografie());

        /// <summary>Pachet chirurgical – extracție cu suturare.</summary>
        public static ServicePackage PachetChirurgicalExtractie() =>
            new ServicePackage
            {
                Name        = "Pachet Extracție Chirurgicală",
                Category    = "Chirurgie orală",
                Description = "Anestezie + extracție + sutură"
            }
            .Add(AnestezieLocala())
            .Add(ExtractieSimpa())
            .Add(Sutura());

        /// <summary>
        /// Pachet implant complet – demonstrează Composite imbricat:
        /// conține un sub-pachet (chirurgical) + servicii protetice.
        /// Discount 10% pe tot pachetul.
        /// </summary>
        public static ServicePackage PachetImplantComplet()
        {
            // Sub-pachet chirurgical (Composite în Composite)
            var chirurgical = new ServicePackage
            {
                Name        = "Faza Chirurgicală",
                Category    = "Implantologie",
                Description = "Anestezie + fixare implant"
            }
            .Add(AnestezieLocala())
            .Add(ImplantFixare());

            // Sub-pachet protetic
            var protetic = new ServicePackage
            {
                Name        = "Faza Protetică",
                Category    = "Implantologie",
                Description = "Bont + coroană zirconiu"
            }
            .Add(BontImplant())
            .Add(CoroanaZirconiu());

            // Pachet principal care include ambele sub-pachete
            return new ServicePackage
            {
                Name        = "Pachet Implant Complet (Nobel Biocare)",
                Category    = "Implantologie",
                Description = "Soluție completă: fixare implant + restaurare protetică",
                Discount    = 0.10m   // 10% reducere pe tot pachetul
            }
            .Add(PachetDiagnosticComplet())  // diagnostic pre-implant
            .Add(chirurgical)                 // faza 1
            .Add(protetic);                   // faza 2
        }
    }
}
