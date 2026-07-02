using JMAPI.Interfaces;
using JMAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JMAPI.Controllers
{
    // Which database (live or test) the app is currently serving requests
    // from. Switching (POST) is admin-only, but the current mode (GET) is
    // readable by any authenticated user so the frontend can show a
    // "you're viewing test data" banner to everyone while the switch is
    // active - not just admins. See ActiveDatabaseProvider for how the
    // switch takes effect without a restart.
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseController : ControllerBase
    {
        private readonly IActiveDatabaseProvider _activeDatabase;

        public DatabaseController(IActiveDatabaseProvider activeDatabase)
        {
            _activeDatabase = activeDatabase;
        }

        [HttpGet]
        public IActionResult GetActiveDatabase()
        {
            return Ok(new { isTestMode = _activeDatabase.IsTestMode });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult SetActiveDatabase([FromBody] SetActiveDatabaseRequest request)
        {
            if (request is null)
            {
                return BadRequest("Request body is required.");
            }

            _activeDatabase.SetTestMode(request.IsTestMode);
            return Ok(new { isTestMode = _activeDatabase.IsTestMode });
        }
    }
}
