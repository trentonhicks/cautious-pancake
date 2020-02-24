using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace CodeFlip.CodeJar.Api
{
    public class Code
    {
        [JsonIgnore]
        public int ID { get; set; }
        public string StringValue { get; set; }

        public string State { get; set; }

        [JsonIgnore]
        public int SeedValue { get; set; }
    }
}