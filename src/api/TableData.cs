using System;
using System.Collections.Generic;

namespace CodeFlip.CodeJar.Api
{
    public class TableData
    {
        public TableData(int campaignSize, int pageSize, int pageNumber, List<Code> codes)
        {
            PageCount = campaignSize / 10;
            if(campaignSize % pageSize > 0)
            {
                PageCount++;
            }

            PageNumber = pageNumber;
            Codes = new List<Code>(codes);
        }
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        List<Code> Codes { get; set; }
    }
}