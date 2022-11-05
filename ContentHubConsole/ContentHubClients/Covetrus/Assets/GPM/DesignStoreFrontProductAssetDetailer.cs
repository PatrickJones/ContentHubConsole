using ContentHubConsole.Assets;
using ContentHubConsole.Taxonomies;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM
{
    public class DesignStoreFrontProductAssetDetailer : BaseDetailer
    {
        public DesignStoreFrontProductAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            long gpmStoreFrontBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.GPM.StoreFront")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(gpmStoreFrontBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);

                    Console.WriteLine($"New asset {asset.Asset.Id} from path {asset.OriginPath}");
                }
                catch (Exception ex)
                {
                    asset.Errors.Add(ex.Message);
                    _failedAssets.Add(asset);
                    continue;
                }

            }

            return results.Count;
        }

    }
}
