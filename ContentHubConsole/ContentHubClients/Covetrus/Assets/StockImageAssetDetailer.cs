using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets
{
    public class StockImageAssetDetailer : BaseDetailer
    {
        public static readonly string UploadPath = @"C:\Users\ptjhi\Dropbox (Covetrus)\Consumer Creative\SmartPak\IMAGES\Generic Images";

        public StockImageAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long spDesignBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.Shared")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long designId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.StockImages")).FirstOrDefault().Id.Value;
            long designUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Equals("CV.AssetUsage.Design")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(spDesignBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(designId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(designUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    await AddTagFromPath(asset);

                    SetStockImages(asset);
                    SetUsages(asset);
                    UpdateAssetType(asset);
                    SetSeason(asset);

                    var log = $"New asset {asset.Asset.Id} from path {asset.OriginPath}";
                    Console.WriteLine(log);
                    FileLogger.Log("UpdateAllAssets", log);
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

        internal void SetUsages(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("icon", StringComparison.InvariantCultureIgnoreCase) || asset.OriginPath.Contains("font", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Icon"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }

            if (asset.OriginPath.Contains("infographic", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Equals("CV.AssetUsage.Infograhic"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private async Task AddTagFromPath(CovetrusAsset asset)
        {
            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Generic Images", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag);
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }
        }

        private void UpdateAssetType(CovetrusAsset asset)
        {
            var pathDirectoryCount = asset.OriginPath.Split('\\').Count();

            if (asset.OriginPath.Split('\\').Any(a => a.Contains("stock", StringComparison.InvariantCultureIgnoreCase))
                 || asset.OriginPath.Split('\\').Any(a => a.Contains("getty", StringComparison.InvariantCultureIgnoreCase)))
            {
                var typeCheck = asset.OriginPath.Split('\\').LastOrDefault();

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Contains("getty", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Getty")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("shutter", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Shutterstock")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.StockImages")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                }
            }
        }
    }
}