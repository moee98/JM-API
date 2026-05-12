using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using JMAPI.Database;
using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace JMAPI.Services
{
    public class IntegrationService(AppDbContext db, IConfiguration config, IHttpClientFactory httpClientFactory) : IIntegrationService
    {
        private readonly AppDbContext _db = db;
        private readonly IConfiguration _config = config;
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

        // ── Token management ─────────────────────────────────────────────────

        public async Task<IntegrationToken?> GetTokenAsync(string provider)
            => await _db.IntegrationTokens.FirstOrDefaultAsync(t => t.Provider == provider);

        public async Task SaveTokenAsync(string provider, string accessToken, string? refreshToken, int expiresInSeconds, string? scope = null)
        {
            var token = await _db.IntegrationTokens.FirstOrDefaultAsync(t => t.Provider == provider);
            if (token is null)
            {
                token = new IntegrationToken { Provider = provider, AccessToken = accessToken };
                _db.IntegrationTokens.Add(token);
            }
            token.AccessToken = accessToken;
            if (refreshToken is not null) token.RefreshToken = refreshToken;
            token.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
            token.Scope = scope;
            token.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task DeleteTokenAsync(string provider)
        {
            var token = await _db.IntegrationTokens.FirstOrDefaultAsync(t => t.Provider == provider);
            if (token is not null)
            {
                _db.IntegrationTokens.Remove(token);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<string> GetValidAccessTokenAsync(string provider)
        {
            var token = await GetTokenAsync(provider) ?? throw new InvalidOperationException($"{provider} is not connected.");

            // Refresh if within 5 minutes of expiry
            if (token.ExpiresAt <= DateTime.UtcNow.AddMinutes(5))
            {
                if (string.IsNullOrEmpty(token.RefreshToken))
                    throw new InvalidOperationException($"{provider} token has expired and no refresh token is available. Please reconnect.");

                await (provider == "square" ? RefreshSquareTokenAsync(token) : RefreshEbayTokenAsync(token));
                token = await GetTokenAsync(provider) ?? throw new InvalidOperationException($"{provider} token missing after refresh.");
            }

            return token.AccessToken;
        }

        // ── Square OAuth ─────────────────────────────────────────────────────

        public string BuildSquareAuthUrl()
        {
            var appId = _config["Square:AppId"];
            var redirectUri = Uri.EscapeDataString(_config["Square:RedirectUri"] ?? "");
            return $"https://connect.squareup.com/oauth2/authorize?client_id={appId}&scope=PAYMENTS_READ+ORDERS_READ&redirect_uri={redirectUri}&response_type=code";
        }

        public async Task<bool> ExchangeSquareCodeAsync(string code)
        {
            var appId = _config["Square:AppId"];
            var appSecret = _config["Square:AppSecret"];
            var redirectUri = _config["Square:RedirectUri"];

            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                client_id = appId,
                client_secret = appSecret,
                code,
                redirect_uri = redirectUri,
                grant_type = "authorization_code"
            };

            var response = await client.PostAsJsonAsync("https://connect.squareup.com/oauth2/token", payload);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = json.GetProperty("access_token").GetString() ?? "";
            var refreshToken = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresAt = json.TryGetProperty("expires_at", out var exp) ? exp.GetString() : null;

            int expiresIn = 7776000; // Square tokens default to 90 days
            if (expiresAt is not null && DateTime.TryParse(expiresAt, out var expDate))
                expiresIn = (int)(expDate - DateTime.UtcNow).TotalSeconds;

            await SaveTokenAsync("square", accessToken, refreshToken, expiresIn);
            return true;
        }

        private async Task RefreshSquareTokenAsync(IntegrationToken token)
        {
            var client = _httpClientFactory.CreateClient();
            var payload = new
            {
                client_id = _config["Square:AppId"],
                client_secret = _config["Square:AppSecret"],
                refresh_token = token.RefreshToken,
                grant_type = "refresh_token"
            };

            var response = await client.PostAsJsonAsync("https://connect.squareup.com/oauth2/token", payload);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Failed to refresh Square token.");

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = json.GetProperty("access_token").GetString() ?? "";
            var refreshToken = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : token.RefreshToken;
            var expiresAt = json.TryGetProperty("expires_at", out var exp) ? exp.GetString() : null;
            int expiresIn = 7776000;
            if (expiresAt is not null && DateTime.TryParse(expiresAt, out var expDate))
                expiresIn = (int)(expDate - DateTime.UtcNow).TotalSeconds;

            await SaveTokenAsync("square", accessToken, refreshToken, expiresIn);
        }

        // ── eBay OAuth ───────────────────────────────────────────────────────

        public string BuildEbayAuthUrl()
        {
            var clientId = _config["eBay:ClientId"];
            var ruName = Uri.EscapeDataString(_config["eBay:RuName"] ?? "");
            var scope = Uri.EscapeDataString(
                "https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly " +
                "https://api.ebay.com/oauth/api_scope/sell.inventory.readonly");
            return $"https://auth.ebay.com/oauth2/authorize?client_id={clientId}&redirect_uri={ruName}&response_type=code&scope={scope}";
        }

        public async Task<bool> ExchangeEbayCodeAsync(string code)
        {
            var clientId = _config["eBay:ClientId"];
            var clientSecret = _config["eBay:ClientSecret"];
            var redirectUri = _config["eBay:RedirectUri"];

            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri ?? ""
            });

            var response = await client.PostAsync("https://api.ebay.com/identity/v1/oauth2/token", body);
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = json.GetProperty("access_token").GetString() ?? "";
            var refreshToken = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            int expiresIn = json.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 7200;
            var scope = json.TryGetProperty("scope", out var sc) ? sc.GetString() : null;

            await SaveTokenAsync("ebay", accessToken, refreshToken, expiresIn, scope);
            return true;
        }

        private async Task RefreshEbayTokenAsync(IntegrationToken token)
        {
            var clientId = _config["eBay:ClientId"];
            var clientSecret = _config["eBay:ClientSecret"];
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = token.RefreshToken ?? ""
            });

            var response = await client.PostAsync("https://api.ebay.com/identity/v1/oauth2/token", body);
            if (!response.IsSuccessStatusCode) throw new InvalidOperationException("Failed to refresh eBay token.");

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = json.GetProperty("access_token").GetString() ?? "";
            int expiresIn = json.TryGetProperty("expires_in", out var ei) ? ei.GetInt32() : 7200;

            await SaveTokenAsync("ebay", accessToken, token.RefreshToken, expiresIn, token.Scope);
        }

        // ── Square data ──────────────────────────────────────────────────────

        public async Task<SquareTransactionsResult> GetSquareTransactionsAsync()
        {
            var accessToken = await GetValidAccessTokenAsync("square");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Square-Version", "2024-01-17");

            var response = await client.GetAsync("https://connect.squareup.com/v2/payments?sort_order=DESC&limit=25");
            if (!response.IsSuccessStatusCode) return new SquareTransactionsResult();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var transactions = new List<SquareTransaction>();

            if (json.TryGetProperty("payments", out var payments))
            {
                foreach (var p in payments.EnumerateArray())
                {
                    var t = new SquareTransaction
                    {
                        Id = p.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "",
                        Status = p.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "",
                        CreatedAt = p.TryGetProperty("created_at", out var ca) && DateTime.TryParse(ca.GetString(), out var dt) ? dt : DateTime.UtcNow
                    };

                    if (p.TryGetProperty("amount_money", out var am))
                    {
                        t.AmountMoney = am.TryGetProperty("amount", out var amt) ? amt.GetInt64() : 0;
                        t.Currency = am.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "GBP" : "GBP";
                    }

                    if (p.TryGetProperty("card_details", out var cd) && cd.TryGetProperty("card", out var card))
                    {
                        t.CardBrand = card.TryGetProperty("card_brand", out var cb) ? cb.GetString() : null;
                        t.Last4 = card.TryGetProperty("last_4", out var l4) ? l4.GetString() : null;
                    }

                    transactions.Add(t);
                }
            }

            return new SquareTransactionsResult { Transactions = transactions };
        }

        public async Task<SquareSummaryResult> GetSquareSummaryAsync()
        {
            var result = await GetSquareTransactionsAsync();
            var now = DateTime.UtcNow;
            var todayStart = now.Date;
            var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            static SquarePeriodSummary Summarise(IEnumerable<SquareTransaction> txns, DateTime from)
            {
                var completed = txns.Where(t => t.CreatedAt >= from && t.Status == "COMPLETED").ToList();
                return new SquarePeriodSummary
                {
                    Amount = completed.Sum(t => t.AmountMoney) / 100m,
                    Count = completed.Count
                };
            }

            return new SquareSummaryResult
            {
                Today = Summarise(result.Transactions, todayStart),
                ThisWeek = Summarise(result.Transactions, weekStart),
                ThisMonth = Summarise(result.Transactions, monthStart)
            };
        }

        // ── eBay data ────────────────────────────────────────────────────────

        public async Task<EbayOrdersResult> GetEbayOrdersAsync()
        {
            var accessToken = await GetValidAccessTokenAsync("ebay");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://api.ebay.com/sell/fulfillment/v1/order?limit=25");
            if (!response.IsSuccessStatusCode) return new EbayOrdersResult();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var orders = new List<EbayOrder>();

            if (json.TryGetProperty("orders", out var ordersEl))
            {
                foreach (var o in ordersEl.EnumerateArray())
                {
                    var order = new EbayOrder
                    {
                        OrderId = o.TryGetProperty("orderId", out var oid) ? oid.GetString() ?? "" : "",
                        Status = o.TryGetProperty("orderFulfillmentStatus", out var st) ? st.GetString() ?? "" : "",
                        CreatedAt = o.TryGetProperty("creationDate", out var cd) && DateTime.TryParse(cd.GetString(), out var dt) ? dt : DateTime.UtcNow,
                        ShipmentComplete = o.TryGetProperty("orderFulfillmentStatus", out var fs) && fs.GetString() == "FULFILLED"
                    };

                    if (o.TryGetProperty("buyer", out var buyer))
                        order.BuyerUsername = buyer.TryGetProperty("username", out var un) ? un.GetString() : null;

                    if (o.TryGetProperty("pricingSummary", out var pricing) && pricing.TryGetProperty("total", out var total))
                    {
                        order.Total = total.TryGetProperty("value", out var val) && decimal.TryParse(val.GetString(), out var d) ? d : 0;
                        order.Currency = total.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "GBP" : "GBP";
                    }

                    if (o.TryGetProperty("lineItems", out var lineItems))
                    {
                        foreach (var li in lineItems.EnumerateArray())
                        {
                            var item = new EbayLineItem
                            {
                                Title = li.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                                Quantity = li.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1
                            };
                            if (li.TryGetProperty("lineItemCost", out var cost) && cost.TryGetProperty("value", out var v))
                                item.Price = decimal.TryParse(v.GetString(), out var p) ? p : 0;
                            order.LineItems.Add(item);
                        }
                    }

                    orders.Add(order);
                }
            }

            return new EbayOrdersResult { Orders = orders };
        }

        public async Task<EbayListingsResult> GetEbayListingsAsync()
        {
            var accessToken = await GetValidAccessTokenAsync("ebay");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync("https://api.ebay.com/sell/inventory/v1/listing?limit=25");
            if (!response.IsSuccessStatusCode) return new EbayListingsResult();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var listings = new List<EbayListing>();

            if (json.TryGetProperty("listingRequests", out var listingsEl))
            {
                foreach (var l in listingsEl.EnumerateArray())
                {
                    var listing = new EbayListing
                    {
                        ListingId = l.TryGetProperty("listingId", out var lid) ? lid.GetString() ?? "" : "",
                        Status = l.TryGetProperty("status", out var st) ? st.GetString() ?? "" : ""
                    };

                    if (l.TryGetProperty("price", out var price))
                    {
                        listing.Price = price.TryGetProperty("value", out var v) && decimal.TryParse(v.GetString(), out var d) ? d : 0;
                        listing.Currency = price.TryGetProperty("currency", out var cur) ? cur.GetString() ?? "GBP" : "GBP";
                    }

                    listings.Add(listing);
                }
            }

            return new EbayListingsResult { Listings = listings };
        }

        public async Task<EbaySummaryResult> GetEbaySummaryAsync()
        {
            var ordersTask = GetEbayOrdersAsync();
            var listingsTask = GetEbayListingsAsync();
            await Task.WhenAll(ordersTask, listingsTask);

            var orders = (await ordersTask).Orders;
            var listings = (await listingsTask).Listings;

            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var thisMonthOrders = orders.Where(o => o.CreatedAt >= monthStart).ToList();

            return new EbaySummaryResult
            {
                ActiveListings = listings.Count,
                OrdersThisMonth = thisMonthOrders.Count,
                RevenueThisMonth = thisMonthOrders.Sum(o => o.Total),
                PendingShipments = orders.Count(o => !o.ShipmentComplete)
            };
        }
    }
}
