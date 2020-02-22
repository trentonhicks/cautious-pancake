using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Storage.Blob;
using System.Threading;
using System.Threading.Tasks;

namespace CodeFlip.CodeJar.Api
{
    public class CodeReader
    {
        public CodeReader(string fileUrl)
        {
            FileUrl = new Uri(fileUrl);
        }

        public Uri FileUrl { get; private set; }

        public List<Code> GenerateCodesFromFile(long[] offsetRange)
        {
            var codes = new List<Code>();
            var fileAccess = new CloudBlockBlob(FileUrl);

            for(var i = offsetRange[0]; i < offsetRange[1]; i += 4)
            {
                var bytes = new byte[4];
                fileAccess.DownloadRangeToByteArray(bytes, index: 0, blobOffset: i, length: 4);
                var seedValue = BitConverter.ToInt32(bytes, 0);
                var code = new Code()
                {
                    SeedValue = seedValue
                };
                codes.Add(code);
            }

            return codes;
        }
    }
}