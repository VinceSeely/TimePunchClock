using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TimeApi.Services;
using TimeClock.Client;

namespace TimeApi.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimePunchController(TimePunchRepository punchRepository) : ControllerBase
    {


        [HttpGet]
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
        public ActionResult PunchHours(PunchInfo punchInfo)
        {
            punchRepository.InsertPunch(punchInfo);
            var lastPunch = punchRepository.GetLastPunch();

            return Ok(lastPunch);

        }

        [HttpGet("lastpunch")]
        public ActionResult GetLastPunch()
        {
            var lastPunch = punchRepository.GetLastPunch();

            return Ok(lastPunch);
        }
    }

 
}
