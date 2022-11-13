using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.SmartPak
{
    public class PhotographyBasicAssetDetailer : BaseDetailer
    {
        public static readonly string UploadPath = @"C:\Users\ptjhi\Dropbox (Covetrus)\Consumer Creative\SmartPak\IMAGES\Lifestyle";

        public PhotographyBasicAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long spPhotoBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.SmartPak.Photography")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long imageId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.ImageAnalysis")).FirstOrDefault().Id.Value;
            long imageUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Equals("CV.AssetUsage.Advertising")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(spPhotoBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(imageId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(imageUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    await AddTagFromPath(asset);

                    SetStockImages(asset);
                    SetProductUsage(asset);
                    //SetOffsite(asset);
                    //SetOnsite(asset);
                    //SetWebpage(asset);
                    SetUsages(asset);
                    SetSeason(asset);
                    SetAdvertising(asset);
                    //await AddBrandFromPath(asset);

                    SetYear(asset);
                    SetSpecificYear(asset);
                    SetMonth(asset);
                    SetWeek(asset);

                    UpdateAssetType(asset);

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
            if (asset.OriginPath.Contains("Print", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Print"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private async Task AddTagFromPath(CovetrusAsset asset)
        {
            if (asset.OriginPath.Split('\\').Any(a => a.Equals("F22", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('-',' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Lifestyle", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('_', ' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }
        }

        private async Task AddBrandFromPath(CovetrusAsset asset)
        {
            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Brands", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddBrandValue(tag);
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_BRAND_TOASSET);
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

            if (pathSplit.Any(a => a.Equals("F22")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2022")).FirstOrDefault();
                yearId = tax.Id.Value;
            }

            if (yearId != 0)
            {
                asset.SetChildToOneParentRelation(yearId, CovetrusRelationNames.RELATION_YEAR_TOASSET);
            }
        }

        private void UpdateAssetType(CovetrusAsset asset)
        {
            var pathDirectoryCount = asset.OriginPath.Split('\\').Count();

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Lifestyle", StringComparison.InvariantCultureIgnoreCase)))
            {
                var assetId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Lifestyle")).FirstOrDefault().Id.Value;
                asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Labels", StringComparison.InvariantCultureIgnoreCase)))
            {
                var typeCheck = asset.OriginPath.Split('\\').Skip(7).Take(1).FirstOrDefault();

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Equals("labels", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Label")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Contains("font", StringComparison.InvariantCultureIgnoreCase))
                && asset.OriginPath.Split('\\').Any(a => a.Contains("template", StringComparison.InvariantCultureIgnoreCase)))
            {
                var typeCheck = asset.OriginPath.Split('\\').Skip(12).Take(1).FirstOrDefault();

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Equals("banners", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Banner")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Equals("email", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Email")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Equals("social", StringComparison.InvariantCultureIgnoreCase) || typeCheck.Equals("paidsocial", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.SocialMedia")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                }
            }
        }
    }
}