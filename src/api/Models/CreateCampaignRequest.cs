using System;

namespace CodeFlip.CodeJar.Api.Models
{
    public class CreateCampaignRequest
    {
        public string Name { get; set; }
        public int NumberOfCodes { get; set; }
    }
}