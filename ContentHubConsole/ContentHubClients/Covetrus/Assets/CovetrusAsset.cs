using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets
{
    public class CovetrusAsset : AssetBase
    {
        public const string PROPERTY_ORIGINPATH = "OriginPath";

        public CovetrusAsset(IWebMClient webMClient, FileUploadResponse fileUploadResponse) : base(webMClient, fileUploadResponse)
        {
        }

        public CovetrusAsset(IWebMClient webMClient, long assetId) : base(webMClient, assetId)
        {
        }

        public void SetMigrationOriginPath(string originPath = null)
        {
            Asset.SetPropertyValue(PROPERTY_ORIGINPATH, String.IsNullOrEmpty(originPath) ? OriginPath : originPath);
        }
    }
} 
