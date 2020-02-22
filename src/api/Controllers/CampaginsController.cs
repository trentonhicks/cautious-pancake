using System;
using Microsoft.AspNetCore.Mvc;
using CodeFlip.CodeJar.Api.Models;
using Microsoft.Extensions.Configuration;

namespace CodeFlip.CodeJar.Api.Controllers
{
    [ApiController]
    public class CampaginsController : ControllerBase
    {
        public CampaginsController(IConfiguration config)
        {
            _config = config;
        }

        private IConfiguration _config;

        [HttpGet("campaigns")]
        public IActionResult GetAllCampaigns()
        {
            var sql = new SQL(_config.GetConnectionString("Storage"));
            return Ok(sql.GetAllCampaigns());
        }

        [HttpGet("campaigns/{id}")]
        public IActionResult GetCampaign(int id)
        {
            var sql = new SQL(_config.GetConnectionString("Storage"));
            var campaign = sql.GetCampaignByID(id);

            if(campaign == null)
            {
                return NotFound();
            }
            return Ok(campaign);
        }

        [HttpPost("campaigns")]
        public IActionResult CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            if(request.NumberOfCodes >= 1 && request.NumberOfCodes <= 3000)
            {
                var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));
                var codeReader = new CodeReader(fileUrl: _config.GetSection("FileUrls")["SeedBlobUrl"]);

                var campaign = new Campaign()
                {
                    Name = request.Name,
                    Size = request.NumberOfCodes
                };

                // Get the last offset position
                var prevAndNextOffset = sql.UpdateOffset(campaign.Size);

                // Read from the file
                var codes = codeReader.GenerateCodesFromFile(prevAndNextOffset);

                // Create the campaign and insert the codes
                sql.CreateCampaign(campaign, codes);
                return Ok(campaign);
            }

            return BadRequest();
        }

        [HttpDelete("campaigns/{id}")]
        public IActionResult DeactivateCampaign(int id)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));
            var batchExists = sql.DeactivateCampaign(id);

            if(batchExists)
            {
                return Ok();
            }
            return NotFound();
        }

        [HttpGet("campaigns/{id}/codes")]
        public IActionResult GetCodes([FromRoute] int id, [FromQuery] int page)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));
            var tableData = sql.GetCodes(id);
            return Ok(tableData);
        }

        [HttpDelete("campaigns/{campaignId}/codes/{code}")]
        public IActionResult DeactivateCode([FromRoute] int campaignId, [FromRoute] string code)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));
            var codeConverter = new CodeConverter(_config.GetSection("Base26")["Alphabet"]);
            var seedValue = codeConverter.ConvertFromCode(code);
            sql.DeactivateCode(campaignId, seedValue);
            return Ok();
        }

        [HttpPost("codes/{code}")]
        public IActionResult RedeemCode([FromRoute] string code, [FromBody] string email)
        {
            var sql = new SQL(connectionString: _config.GetConnectionString("Storage"));
            var codeConverter = new CodeConverter(_config.GetSection("Base26")["Alphabet"]);
            var seedValue = codeConverter.ConvertFromCode(code);
            var isRedeemed = sql.RedeemCode(seedValue, email);

            if(isRedeemed)
            {
                return Ok();
            }
            return BadRequest();
        }
    }
}
