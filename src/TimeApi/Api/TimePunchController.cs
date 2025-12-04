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


        [HttpGet]
        [Authorize(Policy = AuthorizationPolicies.HasAccount)]
        public ActionResult GetHours(DateTime start, DateTime end)
        {
            var results = punchRepository.GetPunchRecords(start, end);
            if (results.IsNullOrEmpty())
            {
                return Ok(Array.Empty<PunchRecord>());
            }
            return Ok(results);
        }

        [HttpPost()]
        [Authorize]
        public ActionResult PunchHours(PunchInfo punchInfo)
        {
            punchRepository.InsertPunch(punchInfo);
            var lastPunch = punchRepository.GetLastPunch();

            return Ok(lastPunch);

        }

        [HttpGet("lastpunch")]
        [Authorize]
        public ActionResult GetLastPunch()
        {
            var lastPunch = punchRepository.GetLastPunch();

            if (lastPunch == null)
            {
                return NotFound(new { message = "No punch records found" });
            }

            return Ok(lastPunch);
        }
    }

 
}
