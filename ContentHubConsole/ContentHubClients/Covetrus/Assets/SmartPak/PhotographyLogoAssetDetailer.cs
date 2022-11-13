using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.SmartPak
{
    public class PhotographyLogoAssetDetailer : BaseDetailer
    {
        public static readonly string UploadPath = @"C:\Users\ptjhi\Dropbox (Covetrus)\Consumer Creative\SmartPak\IMAGES\Logo_Art\";

        public PhotographyLogoAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long spPhotoBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.SmartPak.Photography")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long logoId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Logo")).FirstOrDefault().Id.Value;
            long logoUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Equals("CV.AssetUsage.Logo")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(spPhotoBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(logoId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(logoUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    SetStockImages(asset);
                    SetProductUsage(asset);
                    //SetOffsite(asset);
                    //SetOnsite(asset);
                    //SetWebpage(asset);
                    SetSeason(asset);
                    //SetAdvertising(asset);
                    await AddManufacturerFromPath(asset);

                    SetYear(asset);
                    SetSpecificYear(asset);
                    SetMonth(asset);
                    SetWeek(asset);

                    //UpdateAssetType(asset);

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
            if (asset.OriginPath.Contains("Template", StringComparison.InvariantCultureIgnoreCase) || asset.OriginPath.Contains("font", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Templates"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }

            if (asset.OriginPath.Contains("font", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Font"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }

            if (asset.OriginPath.Contains("style_guide", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("StyleGuide"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }

            if (asset.OriginPath.Contains("sow", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("SOW"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private async Task AddManufacturerFromPath(CovetrusAsset asset)
        {
            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Logo_Art", StringComparison.InvariantCultureIgnoreCase)) 
                || asset.OriginPath.Split('\\').Any(a => a.Equals("Logos", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddManufacturerValue(tag);
                if (tagId != 0)
                {
                    asset.SetChildToOneParentRelation(tagId, CovetrusRelationNames.RELATION_MANUFACTURER_TOASSET);
                }
            }
        }

        private void SetSpecificYear(CovetrusAsset asset)
        {
            long yearId = 0;

            var pathSplit = asset.OriginPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("2021_ColiCare")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2021")).FirstOrDefault();
                yearId = tax.Id.Value;
            }

            if (yearId != 0)
            {
                asset.SetChildToOneParentRelation(yearId, CovetrusRelationNames.RELATION_YEAR_TOASSET);
            }
        }
    }
}