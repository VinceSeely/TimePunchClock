using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TimeClock.Client;

namespace TimeApi.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimePunchController() : ControllerBase
    {


        [HttpGet("hours")]
        public ActionResult GetHours(DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        [HttpPost()]
        public ActionResult PunchHours(PunchInfo punchInfo)
        {
            throw new NotImplementedException();
        }
    }

 
}
