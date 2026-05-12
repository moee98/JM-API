using JMAPI.Models;

namespace JMAPI.Interfaces
{
    public interface IIntegrationService
    {
        // Token management
        Task<IntegrationToken?> GetTokenAsync(string provider);
        Task SaveTokenAsync(string provider, string accessToken, string? refreshToken, int expiresInSeconds, string? scope = null);
        Task<string> GetValidAccessTokenAsync(string provider);
        Task DeleteTokenAsync(string provider);

        // Square OAuth
        string BuildSquareAuthUrl();
        Task<bool> ExchangeSquareCodeAsync(string code);

        // eBay OAuth
        string BuildEbayAuthUrl();
        Task<bool> ExchangeEbayCodeAsync(string code);

        // Square data
        Task<SquareTransactionsResult> GetSquareTransactionsAsync();
        Task<SquareSummaryResult> GetSquareSummaryAsync();

        // eBay data
        Task<EbayOrdersResult> GetEbayOrdersAsync();
        Task<EbayListingsResult> GetEbayListingsAsync();
        Task<EbaySummaryResult> GetEbaySummaryAsync();
    }

    // ── Square result types ──────────────────────────────────────────────────

    public class SquareTransaction
    {
        public string Id { get; set; } = "";
        public long AmountMoney { get; set; }       // in smallest currency unit (pence)
        public string Currency { get; set; } = "GBP";
        public string Status { get; set; } = "";
        public string? CardBrand { get; set; }
        public string? Last4 { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SquarePeriodSummary
    {
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class SquareSummaryResult
    {
        public SquarePeriodSummary Today { get; set; } = new();
        public SquarePeriodSummary ThisWeek { get; set; } = new();
        public SquarePeriodSummary ThisMonth { get; set; } = new();
    }

    public class SquareTransactionsResult
    {
        public List<SquareTransaction> Transactions { get; set; } = [];
    }

    // ── eBay result types ────────────────────────────────────────────────────

    public class EbayLineItem
    {
        public string Title { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class EbayOrder
    {
        public string OrderId { get; set; } = "";
        public string? BuyerUsername { get; set; }
        public decimal Total { get; set; }
        public string Currency { get; set; } = "GBP";
        public string Status { get; set; } = "";
        public bool ShipmentComplete { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<EbayLineItem> LineItems { get; set; } = [];
    }

    public class EbayListing
    {
        public string ListingId { get; set; } = "";
        public string Title { get; set; } = "";
        public decimal Price { get; set; }
        public string Currency { get; set; } = "GBP";
        public int Quantity { get; set; }
        public string Status { get; set; } = "";
    }

    public class EbayOrdersResult
    {
        public List<EbayOrder> Orders { get; set; } = [];
    }

    public class EbayListingsResult
    {
        public List<EbayListing> Listings { get; set; } = [];
    }

    public class EbaySummaryResult
    {
        public int ActiveListings { get; set; }
        public int OrdersThisMonth { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public int PendingShipments { get; set; }
    }
}
