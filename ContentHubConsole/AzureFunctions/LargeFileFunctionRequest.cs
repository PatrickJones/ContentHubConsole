using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.AzureFunctions
{
    public class LargeFileFunctionRequest
    {
        public LargeFileFunctionRequest(string contentHubHostName, string contentHubToken, string fileContent, long fileSize, string filename, string mediaType, string uploadConfiguration)
        {
            ContentHubHostName = contentHubHostName;
            ContentHubToken = contentHubToken;
            FileContent = fileContent;
            FileSize = fileSize;
            Filename = filename;
            MediaType = mediaType;
            UploadConfiguration = uploadConfiguration;
        }

        public string ContentHubHostName { get; set; }
        public string ContentHubToken { get; set; }
        public string FileContent { get; set; }
        public long FileSize { get; set; }
        public string Filename { get; set; }
        public string MediaType { get; set; }
        public string UploadConfiguration { get; set; }
    }
}
