using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.LogicApps
{
    public class LogicAppRequest
    {
        public LogicAppRequest(string contentHubHostName, string contentHubToken, string contentHubUploadConfiguration, string dropboxFolderPath, bool isBoxRoot)
        {
            ContentHubHostName = contentHubHostName;
            ContentHubToken = contentHubToken;
            ContentHubUploadConfiguration = contentHubUploadConfiguration;
            DropboxFolderPath = dropboxFolderPath;
            IsBoxRoot = isBoxRoot;
        }

        public string ContentHubHostName { get; set; }
        public string ContentHubToken { get; set; }
        public string ContentHubUploadConfiguration { get; set; }
        public string DropboxFolderPath { get; set; }
        public string Filename { get; set; }
        public bool IsBoxRoot { get; set; }
    }
}
