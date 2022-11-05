using ContentHubConsole.Assets;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM;
using ContentHubConsole.ContentHubClients.Covetrus.Taxonomy;
using ContentHubConsole.Taxonomies;
using Nito.AsyncEx;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets
{
    public abstract class BaseDetailer
    {
        private readonly IWebMClient _webMClient;
        internal CovetrusTaxonomyManager _taxonomyManager;

        internal ICollection<CovetrusAsset> _covetrusAsset = new List<CovetrusAsset>();
        internal ICollection<CovetrusAsset> _failedAssets = new List<CovetrusAsset>();

        public BaseDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses)
        {
            _webMClient = webMClient;
            _taxonomyManager = new CovetrusTaxonomyManager(webMClient);
            CreateAssets(fileUploadResponses);
        }

        private void CreateAssets(ICollection<FileUploadResponse> fileUploadResponses)
        {
            for (int i = 0; i < fileUploadResponses.Count; i++)
            {
                var resp = fileUploadResponses.ElementAt(i);
                var asset = new CovetrusAsset(_webMClient, resp);

                _covetrusAsset.Add(asset);
            }
        }

        public abstract Task<long> UpdateAllAssets();
        public async virtual Task SaveAllAssets()
        {
            var tasks = new List<Task>();

            foreach (var asset in _covetrusAsset)
            {
                tasks.Add(asset.SaveAsset());
            }

            await tasks.WhenAll();
        }

        public string GetDescriptionFromOriginPath(string path)
        {
            if (!String.IsNullOrEmpty(path))
            {
                var split = path.Split('\\');
                return DescriptionBuilder.BuildBulletString(split.Skip(3).ToArray());
            }
            
            return String.Empty;
        }
    }
}
