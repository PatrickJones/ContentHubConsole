using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.Assets
{
    public class FileUploadResponse
    {
        public FileUploadResponse(long assetId, string localPath)
        {
            AssetId = assetId;
            LocalPath = localPath;
        }

        public long AssetId { get; set; }
        public string LocalPath { get; set; }

        public bool SuccessfulUpload { get { return AssetId > 0; } }
    }
}
