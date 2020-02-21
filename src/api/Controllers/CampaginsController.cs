using System;
using Microsoft.AspNetCore.Mvc;
using CodeFlip.CodeJar.Api.Models;

namespace CodeFlip.CodeJar.Api.Controllers
{
    [ApiController]
    public class CampaginsController : ControllerBase
    {
        [HttpGet("campaigns")]
        public IActionResult GetAllCampaigns()
        {
            return Ok(
                new []
                {
                    new { id = "1", name = "campaign 01", numberOfCodes = "10", dateActive = DateTime.Now, dateExpires = DateTime.Now.AddDays(1) },
                    new { id = "2", name = "campaign 02", numberOfCodes = "100", dateActive = DateTime.Now.AddDays(1), dateExpires = DateTime.Now.AddDays(2) },
                    new { id = "3", name = "campaign 03", numberOfCodes = "1000", dateActive = DateTime.Now.AddDays(2), dateExpires = DateTime.Now.AddDays(3) }
                }
            );
        }

        [HttpGet("campaigns/{id}")]
        public IActionResult GetCampaign(int id)
        {
            return Ok(new { id = "1", name = "campaign 01", numberOfCodes = "10", dateActive = DateTime.Now, dateExpires = DateTime.Now.AddDays(1) });
        }

        [HttpPost("campaigns")]
        public IActionResult CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            return Ok();
        }

        [HttpDelete("campaigns/{id}")]
        public IActionResult DeactivateCampaign(int id)
        {
            return Ok();
        }

        [HttpGet("campaigns/{id}/codes")]
        public IActionResult GetCodes([FromRoute] int id, [FromQuery] int page)
        {
            return Ok(
                new
                {
                    pageNumber = page,
                    pageCount = 1,
                    codes = new[] { new { stringValue = "ASKJSJQ", state = 1 }, new { stringValue = "AWEORJZ", state = 2 }}
                }
            );
        }

        [HttpDelete("campaigns/{campaignId}/codes/{code}")]
        public IActionResult DeactivateCode([FromRoute] int campaignId, [FromRoute] string code)
        {
            return Ok();
        }

        [HttpPost("codes/{code}")]
        public IActionResult RedeemCode([FromRoute] string code)
        {
            return Ok();
        }
    }
}
