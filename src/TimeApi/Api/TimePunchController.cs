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


        [HttpGet("hours")]
        public ActionResult GetHours(DateTime start, DateTime end)
        {
            var results = punchRepository.GetPunchRecords(start, end);
            if (!results.IsNullOrEmpty())
            {
                return BadRequest("No results found for date range");
            }
            return Ok(results);
        }

        [HttpPost()]
        public ActionResult PunchHours(PunchInfo punchInfo)
        {
            punchRepository.InsertPunch(punchInfo);

            return Ok();

        }
    }

 
}
