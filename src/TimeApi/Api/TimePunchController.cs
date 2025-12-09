using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TimeApi.Constants;
using TimeApi.Services;
using TimeClock.Client;

namespace TimeApi.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimePunchController(ITimePunchRepository punchRepository) : ControllerBase
    {
        private string GetAuthId()
        {
            // Check if authentication is disabled (development mode)
            if (User.Identity?.IsAuthenticated != true)
            {
                return "dev-user";
            }

            // Try to get 'oid' (Object ID) claim from Azure AD
            var oidClaim = User.FindFirst("oid")
                ?? User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

            if (oidClaim != null)
                return oidClaim.Value;

            // Fallback to 'sub' (Subject) for other identity providers
            var subClaim = User.FindFirst("sub")
                ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

            if (subClaim != null)
                return subClaim.Value;

            throw new UnauthorizedAccessException("User identifier not found in token claims");
        }

        [HttpGet]
        [Authorize]
        public ActionResult GetHours(DateTime start, DateTime end)
        {
            var authId = GetAuthId();
            var results = punchRepository.GetPunchRecords(start, end, authId);
            if (results == null || !results.Any())
            {
                return Ok(Array.Empty<PunchRecord>());
            }
            return Ok(results);
        }

        [HttpPost()]
        [Authorize]
        public ActionResult PunchHours(PunchInfo punchInfo)
        {
            var authId = GetAuthId();
            punchRepository.InsertPunch(punchInfo, authId);
            var lastPunch = punchRepository.GetLastPunch(authId);

            return Ok(lastPunch);

        }

        [HttpGet("lastpunch")]
        [Authorize]
        public ActionResult GetLastPunch()
        {
            var authId = GetAuthId();
            var lastPunch = punchRepository.GetLastPunch(authId);

            if (lastPunch == null)
            {
                return NotFound(new { message = "No punch records found" });
            }

            return Ok(lastPunch);
        }

        [HttpPut]
        [Authorize]
        public ActionResult UpdatePunch(PunchUpdateDto updateDto)
        {
            try
            {
                var authId = GetAuthId();
                var updatedPunch = punchRepository.UpdatePunch(updateDto, authId);
                return Ok(updatedPunch);
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{punchId}")]
        [Authorize]
        public ActionResult DeletePunch(Guid punchId)
        {
            try
            {
                var authId = GetAuthId();
                punchRepository.DeletePunch(punchId, authId);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }
    }


}
