using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace JMAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VersionController : ControllerBase
    {
        // Anonymous by design: the app runs on a trusted LAN/Tailscale network,
        // the dashboard shows this next to its own version, and it doubles as a
        // lightweight health probe.
        [HttpGet]
        public IActionResult Get()
        {
            // InformationalVersion carries the csproj <Version> (stamped by
            // release-please); the SDK may append "+<commit-sha>", so trim it.
            var informational = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            var version = informational?.Split('+')[0] ?? "unknown";
            return Ok(new { version });
        }
    }
}
