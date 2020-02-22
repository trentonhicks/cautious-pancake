﻿using System;
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
