// ═══════════════════════════════════════════════════════════════════════
//  ADAPTER PATTERN
//  Domeniu: Integrarea gateway-urilor de plată externe
//
//  Scenariu: Clinica stomatologică acceptă plăți prin trei furnizori
//  diferiți: MAIB ePay (banca locală), PayNet (procesator local) și
//  Stripe (internațional). Fiecare are propriul API incompatibil.
//  Adapter-ul oferă o interfață unică IPaymentProcessor, astfel încât
//  restul sistemului nu cunoaște și nu depinde de detaliile fiecărui API.
//
//  Participanți:
//  – Target    : IPaymentProcessor    (interfața așteptată de client)
//  – Adaptee   : MaibEPayApi, PayNetApi, StripeApi  (API-uri existente, incompatibile)
//  – Adapter   : MaibAdapter, PayNetAdapter, StripeAdapter
//  – Client    : codul din Program.cs / Façade care apelează IPaymentProcessor
// ═══════════════════════════════════════════════════════════════════════

namespace DentalClinic.Lab4.Adapter
{
    // ───────────────────────────────────────────────────────────────────
    // 1. TARGET – interfața unică așteptată de sistemul clinicii
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Interfața comună pentru orice gateway de plată integrat în sistem.
    /// Clientul (sistemul clinicii) depinde DOAR de această interfață.
    /// </summary>
    public interface IPaymentProcessor
    {
        /// <summary>Identificatorul furnizorului (ex: "MAIB", "PayNet", "Stripe").</summary>
        string ProviderName { get; }

        /// <summary>
        /// Procesează o plată și returnează un rezultat standardizat.
        /// </summary>
        /// <param name="patientName">Numele pacientului (pentru referință).</param>
        /// <param name="amountMDL">Suma în lei moldovenești.</param>
        /// <param name="description">Descrierea plății (ex: "Obturație M1").</param>
        /// <returns>Rezultatul plății: succes, ID tranzacție, mesaj.</returns>
        PaymentResult ProcessPayment(string patientName, decimal amountMDL, string description);

        /// <summary>Verifică starea unei tranzacții anterioare.</summary>
        PaymentStatus CheckStatus(string transactionId);

        /// <summary>Efectuează rambursarea unei plăți procesate anterior.</summary>
        RefundResult Refund(string transactionId, decimal amountMDL);
    }

    // ───────────────────────────────────────────────────────────────────
    // 2. VALUE OBJECTS standardizate (returnate de IPaymentProcessor)
    // ───────────────────────────────────────────────────────────────────

    public enum PaymentStatus { Pending, Completed, Failed, Refunded }

    public class PaymentResult
    {
        public bool         Success       { get; init; }
        public string       TransactionId { get; init; } = string.Empty;
        public string       Provider      { get; init; } = string.Empty;
        public decimal      Amount        { get; init; }
        public string       Currency      { get; init; } = "MDL";
        public string       Message       { get; init; } = string.Empty;
        public DateTime     ProcessedAt   { get; init; }
        public PaymentStatus Status       { get; init; }

        public override string ToString() =>
            $"[{Provider}] {(Success ? "✅" : "❌")} " +
            $"TxID:{TransactionId} | {Amount:F2} {Currency} | {Message}";
    }

    public class RefundResult
    {
        public bool    Success       { get; init; }
        public string  RefundId      { get; init; } = string.Empty;
        public decimal AmountRefunded { get; init; }
        public string  Message       { get; init; } = string.Empty;

        public override string ToString() =>
            $"Rambursare {(Success ? "✅" : "❌")}: {AmountRefunded:F2} MDL – {Message}";
    }

    // ───────────────────────────────────────────────────────────────────
    // 3. ADAPTEE #1 – MAIB ePay (API bancar local, în română/rusă)
    //    Clasa existentă cu interfața ei proprie – NU o putem modifica.
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simulează SDK-ul MAIB ePay. API diferit de ce așteptăm noi:
    /// – suma în bani (1 MDL = 100 bani)
    /// – metode cu nume și semnături proprii
    /// – răspunsuri ca dicționar
    /// </summary>
    public class MaibEPayApi
    {
        public Dictionary<string, string> InitiateTransaction(
            string clientName, long amountInBani, string details, string merchantCode)
        {
            Console.WriteLine($"    [MAIB SDK] InitiateTransaction → client={clientName}, " +
                              $"suma={amountInBani} bani, detalii={details}");
            // Simulare răspuns MAIB
            var txId = $"MAIB-{DateTime.Now:yyyyMMddHHmmss}-{Random.Shared.Next(1000,9999)}";
            return new Dictionary<string, string>
            {
                ["status"]         = "00",        // "00" = succes în protocolul MAIB
                ["transaction_id"] = txId,
                ["message"]        = "Tranzacție inițiată cu succes",
                ["timestamp"]      = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }

        public string GetTransactionStatus(string txId)
        {
            // Returnează coduri de stare proprii MAIB
            Console.WriteLine($"    [MAIB SDK] GetTransactionStatus({txId})");
            return "COMPLETED";
        }

        public bool InitiateRefund(string txId, long amountInBani, string reason)
        {
            Console.WriteLine($"    [MAIB SDK] InitiateRefund({txId}, {amountInBani} bani)");
            return true; // simulare succes
        }

        public static readonly string MerchantCode = "DENTACARE-MD-2024";
    }

    // ───────────────────────────────────────────────────────────────────
    // 4. ADAPTEE #2 – PayNet (procesator de plăți local moldovenesc)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simulează SDK-ul PayNet Moldova. Lucrează cu:
    /// – suma ca string cu două zecimale
    /// – metode asincrone simulate (callback-style)
    /// – răspunsuri ca obiect propriu PayNetResponse
    /// </summary>
    public class PayNetResponse
    {
        public int    Code      { get; set; }   // 0 = succes, altceva = eroare
        public string OrderId   { get; set; } = string.Empty;
        public string StatusMsg { get; set; } = string.Empty;
    }

    public class PayNetApi
    {
        private readonly string _serviceKey;

        public PayNetApi(string serviceKey)
        {
            _serviceKey = serviceKey;
        }

        public PayNetResponse CreateOrder(
            string amount, string currency, string description, string customerEmail)
        {
            Console.WriteLine($"    [PayNet SDK] CreateOrder → suma={amount} {currency}, " +
                              $"desc={description}, email={customerEmail}");
            return new PayNetResponse
            {
                Code    = 0,
                OrderId = $"PNT-{Guid.NewGuid().ToString()[..8].ToUpper()}",
                StatusMsg = "Order created successfully"
            };
        }

        public int QueryOrderStatus(string orderId)
        {
            Console.WriteLine($"    [PayNet SDK] QueryOrderStatus({orderId})");
            return 2; // 2 = paid în sistemul PayNet
        }

        public PayNetResponse CancelOrder(string orderId, string amount)
        {
            Console.WriteLine($"    [PayNet SDK] CancelOrder({orderId}, {amount})");
            return new PayNetResponse { Code = 0, StatusMsg = "Refund initiated" };
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 5. ADAPTEE #3 – Stripe (internațional, API în engleză, în USD/EUR)
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Simulează Stripe API. Particularități:
    /// – sumele în cenți (USD)
    /// – necesită conversie MDL→USD
    /// – metode complet diferite (charge, retrieve, refund_charge)
    /// </summary>
    public class StripeApi
    {
        private const decimal MdlToUsd = 0.056m; // rata de schimb simulată

        public (string chargeId, bool paid, string failureMessage) CreateCharge(
            long amountCents, string currency, string description, string customerName)
        {
            Console.WriteLine($"    [Stripe SDK] CreateCharge → {amountCents} {currency} cents, " +
                              $"desc={description}");
            var chargeId = $"ch_{Guid.NewGuid().ToString().Replace("-", "")[..24]}";
            return (chargeId, paid: true, failureMessage: string.Empty);
        }

        public string RetrieveCharge(string chargeId)
        {
            Console.WriteLine($"    [Stripe SDK] RetrieveCharge({chargeId})");
            return "succeeded"; // stări Stripe: succeeded, pending, failed
        }

        public (bool refunded, string refundId) RefundCharge(string chargeId, long amountCents)
        {
            Console.WriteLine($"    [Stripe SDK] RefundCharge({chargeId}, {amountCents} cents)");
            var refundId = $"re_{Guid.NewGuid().ToString().Replace("-","")[..24]}";
            return (true, refundId);
        }

        public static decimal ConvertMdlToUsdCents(decimal mdl) =>
            Math.Round(mdl * MdlToUsd * 100, 0); // în cenți USD
    }

    // ───────────────────────────────────────────────────────────────────
    // 6. ADAPTER #1 – MAIB
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adaptează MaibEPayApi la interfața IPaymentProcessor.
    /// Traduce: MDL → bani, coduri MAIB → PaymentStatus, etc.
    /// </summary>
    public class MaibAdapter : IPaymentProcessor
    {
        private readonly MaibEPayApi _maib = new();

        public string ProviderName => "MAIB ePay";

        public PaymentResult ProcessPayment(
            string patientName, decimal amountMDL, string description)
        {
            // Traducere: MDL (lei) → bani (1 leu = 100 bani)
            long amountBani = (long)(amountMDL * 100);

            var response = _maib.InitiateTransaction(
                patientName, amountBani, description, MaibEPayApi.MerchantCode);

            bool success = response["status"] == "00";
            return new PaymentResult
            {
                Success       = success,
                TransactionId = response["transaction_id"],
                Provider      = ProviderName,
                Amount        = amountMDL,
                Currency      = "MDL",
                Message       = response["message"],
                ProcessedAt   = DateTime.Now,
                Status        = success ? PaymentStatus.Completed : PaymentStatus.Failed
            };
        }

        public PaymentStatus CheckStatus(string transactionId)
        {
            var maibStatus = _maib.GetTransactionStatus(transactionId);
            // Traducere cod MAIB → enum standardizat
            return maibStatus switch
            {
                "COMPLETED"  => PaymentStatus.Completed,
                "PENDING"    => PaymentStatus.Pending,
                "REFUNDED"   => PaymentStatus.Refunded,
                _            => PaymentStatus.Failed
            };
        }

        public RefundResult Refund(string transactionId, decimal amountMDL)
        {
            long amountBani = (long)(amountMDL * 100);
            bool ok = _maib.InitiateRefund(transactionId, amountBani, "Rambursare clinică");
            return new RefundResult
            {
                Success        = ok,
                RefundId       = ok ? $"REF-{transactionId}" : string.Empty,
                AmountRefunded = ok ? amountMDL : 0,
                Message        = ok ? "Rambursare MAIB procesată" : "Rambursare MAIB eșuată"
            };
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 7. ADAPTER #2 – PayNet
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adaptează PayNetApi la interfața IPaymentProcessor.
    /// Traduce: decimal MDL → string, coduri PayNet → PaymentStatus.
    /// </summary>
    public class PayNetAdapter : IPaymentProcessor
    {
        private readonly PayNetApi _payNet;

        public PayNetAdapter()
        {
            // Cheia de serviciu ar veni din configurație
            _payNet = new PayNetApi(serviceKey: "PN-DENTACARE-SK-2024");
        }

        public string ProviderName => "PayNet Moldova";

        public PaymentResult ProcessPayment(
            string patientName, decimal amountMDL, string description)
        {
            // PayNet vrea suma ca string și email client (simulat)
            string amount = amountMDL.ToString("F2");
            string email  = $"{patientName.ToLower().Replace(" ", ".")}@pacient.dentacare.md";

            var resp = _payNet.CreateOrder(amount, "MDL", description, email);
            bool success = resp.Code == 0;

            return new PaymentResult
            {
                Success       = success,
                TransactionId = resp.OrderId,
                Provider      = ProviderName,
                Amount        = amountMDL,
                Currency      = "MDL",
                Message       = resp.StatusMsg,
                ProcessedAt   = DateTime.Now,
                Status        = success ? PaymentStatus.Completed : PaymentStatus.Failed
            };
        }

        public PaymentStatus CheckStatus(string transactionId)
        {
            int code = _payNet.QueryOrderStatus(transactionId);
            // Traducere coduri PayNet: 1=pending, 2=paid, 3=failed, 4=refunded
            return code switch
            {
                1 => PaymentStatus.Pending,
                2 => PaymentStatus.Completed,
                3 => PaymentStatus.Failed,
                4 => PaymentStatus.Refunded,
                _ => PaymentStatus.Failed
            };
        }

        public RefundResult Refund(string transactionId, decimal amountMDL)
        {
            var resp = _payNet.CancelOrder(transactionId, amountMDL.ToString("F2"));
            bool ok  = resp.Code == 0;
            return new RefundResult
            {
                Success        = ok,
                RefundId       = ok ? $"REF-{resp.OrderId}" : string.Empty,
                AmountRefunded = ok ? amountMDL : 0,
                Message        = resp.StatusMsg
            };
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 8. ADAPTER #3 – Stripe
    // ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adaptează StripeApi la interfața IPaymentProcessor.
    /// Traduce: MDL → USD cenți, stări Stripe → PaymentStatus.
    /// </summary>
    public class StripeAdapter : IPaymentProcessor
    {
        private readonly StripeApi _stripe = new();

        public string ProviderName => "Stripe";

        public PaymentResult ProcessPayment(
            string patientName, decimal amountMDL, string description)
        {
            // Conversie MDL → USD în cenți (Stripe lucrează în monedă/cenți)
            long cents = (long)StripeApi.ConvertMdlToUsdCents(amountMDL);

            var (chargeId, paid, failureMsg) =
                _stripe.CreateCharge(cents, "usd", description, patientName);

            return new PaymentResult
            {
                Success       = paid,
                TransactionId = chargeId,
                Provider      = ProviderName,
                Amount        = amountMDL,
                Currency      = "MDL",     // păstrăm suma originală în MDL pentru consistență
                Message       = paid ? $"Charged {cents} USD cents" : failureMsg,
                ProcessedAt   = DateTime.Now,
                Status        = paid ? PaymentStatus.Completed : PaymentStatus.Failed
            };
        }

        public PaymentStatus CheckStatus(string transactionId)
        {
            string stripeStatus = _stripe.RetrieveCharge(transactionId);
            return stripeStatus switch
            {
                "succeeded" => PaymentStatus.Completed,
                "pending"   => PaymentStatus.Pending,
                _           => PaymentStatus.Failed
            };
        }

        public RefundResult Refund(string transactionId, decimal amountMDL)
        {
            long cents = (long)StripeApi.ConvertMdlToUsdCents(amountMDL);
            var (refunded, refundId) = _stripe.RefundCharge(transactionId, cents);
            return new RefundResult
            {
                Success        = refunded,
                RefundId       = refundId,
                AmountRefunded = refunded ? amountMDL : 0,
                Message        = refunded
                    ? $"Stripe refund OK ({cents} USD cents)"
                    : "Stripe refund eșuat"
            };
        }
    }

    // ───────────────────────────────────────────────────────────────────
    // 9. PAYMENT PROCESSOR FACTORY – creează adaptorul corect după tip
    // ───────────────────────────────────────────────────────────────────

    public enum PaymentProviderType { Maib, PayNet, Stripe }

    /// <summary>
    /// Factory simplu care returnează adaptorul IPaymentProcessor corect.
    /// Clientul solicită un furnizor și primește interfața uniformă.
    /// </summary>
    public static class PaymentProcessorFactory
    {
        public static IPaymentProcessor Create(PaymentProviderType type) =>
            type switch
            {
                PaymentProviderType.Maib   => new MaibAdapter(),
                PaymentProviderType.PayNet => new PayNetAdapter(),
                PaymentProviderType.Stripe => new StripeAdapter(),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
    }
}
