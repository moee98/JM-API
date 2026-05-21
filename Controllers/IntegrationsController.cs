using JMAPI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JMAPI.Controllers
{
    public record CreateTerminalCheckoutRequest(long AmountMoney, string DeviceId, int JobId);

    [ApiController]
    [Route("api/integrations")]
    [Authorize]
    public class IntegrationsController(IIntegrationService integrationService, IConfiguration config) : ControllerBase
    {
        private readonly IIntegrationService _integrationService = integrationService;
        private readonly IConfiguration _config = config;

        // ── Square status / connect / callback ───────────────────────────────

        [HttpGet("square/status")]
        public async Task<IActionResult> SquareStatus()
        {
            var token = await _integrationService.GetTokenAsync("square");
            return Ok(new { connected = token is not null, expiresAt = token?.ExpiresAt });
        }

        [AllowAnonymous]
        [HttpGet("square/connect")]
        public IActionResult SquareConnect()
        {
            var url = _integrationService.BuildSquareAuthUrl();
            return Redirect(url);
        }

        [AllowAnonymous]
        [HttpGet("square/callback")]
        public async Task<IActionResult> SquareCallback([FromQuery] string? code, [FromQuery] string? error)
        {
            var frontendBase = GetFrontendBase();

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                return Redirect($"{frontendBase}/integrations/square?error=access_denied");

            var success = await _integrationService.ExchangeSquareCodeAsync(code);
            return success
                ? Redirect($"{frontendBase}/integrations/square?connected=true")
                : Redirect($"{frontendBase}/integrations/square?error=token_exchange_failed");
        }

        [HttpDelete("square/disconnect")]
        public async Task<IActionResult> SquareDisconnect()
        {
            await _integrationService.DeleteTokenAsync("square");
            return NoContent();
        }

        // ── Square data endpoints ────────────────────────────────────────────

        [HttpGet("square/transactions")]
        public async Task<IActionResult> SquareTransactions()
        {
            try
            {
                var result = await _integrationService.GetSquareTransactionsAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("square/summary")]
        public async Task<IActionResult> SquareSummary()
        {
            try
            {
                var result = await _integrationService.GetSquareSummaryAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Square Terminal ──────────────────────────────────────────────────

        [HttpGet("square/devices")]
        public async Task<IActionResult> SquareDevices()
        {
            try
            {
                var result = await _integrationService.GetSquareDevicesAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("square/terminal/checkout")]
        public async Task<IActionResult> CreateTerminalCheckout([FromBody] CreateTerminalCheckoutRequest request)
        {
            try
            {
                var result = await _integrationService.CreateTerminalCheckoutAsync(
                    request.AmountMoney, "GBP", request.DeviceId, $"job-{request.JobId}");
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("square/terminal/checkout/{checkoutId}")]
        public async Task<IActionResult> GetTerminalCheckout(string checkoutId)
        {
            try
            {
                var result = await _integrationService.GetTerminalCheckoutAsync(checkoutId);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── eBay status / connect / callback ─────────────────────────────────

        [HttpGet("ebay/status")]
        public async Task<IActionResult> EbayStatus()
        {
            var token = await _integrationService.GetTokenAsync("ebay");
            return Ok(new { connected = token is not null, expiresAt = token?.ExpiresAt });
        }

        [AllowAnonymous]
        [HttpGet("ebay/connect")]
        public IActionResult EbayConnect()
        {
            var url = _integrationService.BuildEbayAuthUrl();
            return Redirect(url);
        }

        [AllowAnonymous]
        [HttpGet("ebay/callback")]
        public async Task<IActionResult> EbayCallback([FromQuery] string? code, [FromQuery] string? error)
        {
            var frontendBase = GetFrontendBase();

            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                return Redirect($"{frontendBase}/integrations/ebay?error=access_denied");

            var success = await _integrationService.ExchangeEbayCodeAsync(code);
            return success
                ? Redirect($"{frontendBase}/integrations/ebay?connected=true")
                : Redirect($"{frontendBase}/integrations/ebay?error=token_exchange_failed");
        }

        [HttpDelete("ebay/disconnect")]
        public async Task<IActionResult> EbayDisconnect()
        {
            await _integrationService.DeleteTokenAsync("ebay");
            return NoContent();
        }

        // ── eBay data endpoints ──────────────────────────────────────────────

        [HttpGet("ebay/orders")]
        public async Task<IActionResult> EbayOrders()
        {
            try
            {
                var result = await _integrationService.GetEbayOrdersAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("ebay/listings")]
        public async Task<IActionResult> EbayListings()
        {
            try
            {
                var result = await _integrationService.GetEbayListingsAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("ebay/summary")]
        public async Task<IActionResult> EbaySummary()
        {
            try
            {
                var result = await _integrationService.GetEbaySummaryAsync();
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private string GetFrontendBase()
        {
            var origins = _config.GetSection("Cors:AllowedOrigins").Get<string[]>();
            if (origins?.Length > 0) return origins[0].TrimEnd('/');

            return Request.Host.Host.Contains("localhost") || Request.Host.Host.Contains("127.0.0.1")
                ? "http://localhost:5173"
                : "http://kazadashboard";
        }
    }
}
