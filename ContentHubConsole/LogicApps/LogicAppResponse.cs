using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.LogicApps
{
    public class LogicAppResponse
    {
        public string BoxPath { get; set; }
        public int ContentHubFileUploadStatus { get; set; }
        public ContentHubReponse ContentHubReponse { get; set; }
        public string FIlename { get; set; }
        public long FileSize { get; set; }
        public string MediaType { get; set; }
    }

    public class ContentHubReponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long Asset_Id { get; set; }
        public string Asset_Identifier { get; set; }
    }
}
