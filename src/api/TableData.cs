using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace CodeFlip.CodeJar.Api
{
    public class TableData
    {
        public TableData(int pageSize, int pageNumber)
        {
            PageSize = pageSize;
            PageNumber = pageNumber;
            RowOffset = pageSize * (PageNumber < 1 ? 0 : PageNumber - 1);
        }
        public int PageNumber { get; set; }
        public int PageCount {get; private set; }
        public List<Code> Codes { get; set; }
        
        [JsonIgnore]
        public int RowOffset { get; private set; }

        [JsonIgnore]
        public int PageSize { get; set; }
        public void SetPageCount(int campaignSize)
        {
            PageCount = campaignSize / 10;
            if(campaignSize % PageSize > 0)
            {
                PageCount++;
            }
        }
    }
}