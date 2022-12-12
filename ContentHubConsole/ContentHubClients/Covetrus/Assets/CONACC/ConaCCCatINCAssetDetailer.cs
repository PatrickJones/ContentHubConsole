using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.CONACC
{
    internal class ConaCCCatINCAssetDetailer : BaseDetailer
    {
        public ConaCCCatINCAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long spPhotoBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.INC")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long imageId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value;
            long imageUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Equals("CV.AssetUsage.Design")).FirstOrDefault().Id.Value;

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

                    SetMultipleBusinessDomains(asset);
                    SetStockImages(asset);
                    //SetProductUsage(asset);
                    //SetOffsite(asset);
                    //SetOnsite(asset);
                    //SetWebpage(asset);
                    SetUsages(asset);
                    SetSeason(asset);
                    //SetAdvertising(asset);
                    //await AddBrandFromPath(asset);

                    SetYear(asset);
                    SetSpecificYear(asset);
                    SetMonth(asset);
                    //SetWeek(asset);

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

        private void SetMultipleBusinessDomains(CovetrusAsset asset)
        {
            var typeFolderLevel = asset.OriginPath.Split('\\').Skip(5).Take(1).FirstOrDefault();
            
            if (typeFolderLevel != null)
            {
                var assetId = typeFolderLevel switch
                {
                    _ when typeFolderLevel.Equals("Equine", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.Equine")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("Human Pharma", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.HumanPharma")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("Large Animal", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.LargeAnimal")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("Med Surg", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.MedSurg")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("Pharma", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.Pharma")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("Strategic Accounts", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.CONACC.StrategicAccounts")).FirstOrDefault().Id.Value,
                    _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                };

                asset.AddChildToManyParentsRelation(assetId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                return;
            }
        }

        public override string RemoveLocalPathPart(string originPath)
        {
            var pathSplit = Program.IsVirtualMachine ? originPath.Split('\\').Skip(2).ToArray() : originPath.Split('\\').Skip(3).ToArray();
            var result = new StringBuilder();

            for (int i = 0; i < pathSplit.Length; i++)
            {
                result.Append($"\\{pathSplit[i]}");
            }

            return result.ToString();
        }

        internal void SetUsages(CovetrusAsset asset)
        {
            var pathStrings = new List<string>();
            var orgSpilt = asset.OriginPath.Split("\\").ToList();

            foreach (var s in orgSpilt)
            {
                var strSplit = s.Split(' ');
                pathStrings.AddRange(strSplit);

                if (s.Contains("_"))
                {
                    var fSplit = s.Split("_");
                    pathStrings.AddRange(fSplit);
                }

                if (s.Equals("Promo", StringComparison.InvariantCultureIgnoreCase) && !pathStrings.Any(a => a == "Promotion"))
                {
                    pathStrings.Add("Promotion");
                }
            }

            foreach (var ps in pathStrings)
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.EndsWith(ps, StringComparison.CurrentCultureIgnoreCase)
                    || w.Identifier.Contains(ps, StringComparison.InvariantCultureIgnoreCase))
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
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('-', ' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("Lifestyle", StringComparison.InvariantCultureIgnoreCase)))
            {
                var tag = Program.IsVirtualMachine ? asset.OriginPath.Split('\\').Skip(6).Take(1).FirstOrDefault() : asset.OriginPath.Split('\\').Skip(8).Take(1).FirstOrDefault();
                var tagId = await _taxonomyManager.AddTagValue(tag.Replace('_', ' '));
                if (tagId != 0)
                {
                    asset.AddChildToManyParentsRelation(tagId, RelationNames.RELATION_TAG_TOASSET);
                }
            }

            if (asset.OriginPath.Split('\\').Any(a => a.Equals("SmartPaks & Supporting", StringComparison.InvariantCultureIgnoreCase)))
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
            if (pathSplit.Any(a => a.Contains("Jan2022")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2022")).FirstOrDefault();
                yearId = tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Contains("2019")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2019")).FirstOrDefault();
                yearId = tax.Id.Value;
            }

            if (yearId != 0)
            {
                asset.SetChildToOneParentRelation(yearId, CovetrusRelationNames.RELATION_YEAR_TOASSET);
            }
        }

        private void UpdateAssetType(CovetrusAsset asset)
        {
            var typeFolderLevel = asset.OriginPath.Split('\\').Skip(6).Take(1).FirstOrDefault();
            var filename = asset.OriginPath.Split('\\').LastOrDefault();
            if (filename.Contains("getty", StringComparison.InvariantCultureIgnoreCase))
            {
                var id = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Getty")).FirstOrDefault().Id.Value;
                asset.SetChildToOneParentRelation(id, RelationNames.RELATION_ASSETTYPE_TOASSET);
                return;
            }

            if (filename.Contains("shutter", StringComparison.InvariantCultureIgnoreCase))
            {
                var id = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Shutterstock")).FirstOrDefault().Id.Value;
                asset.SetChildToOneParentRelation(id, RelationNames.RELATION_ASSETTYPE_TOASSET);
                return;
            }

            if (filename.Contains("stock", StringComparison.InvariantCultureIgnoreCase))
            {
                var id = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.StockImages")).FirstOrDefault().Id.Value;
                asset.SetChildToOneParentRelation(id, RelationNames.RELATION_ASSETTYPE_TOASSET);
                return;
            }

            if (typeFolderLevel != null)
            {
                var assetId = typeFolderLevel switch
                {
                    _ when typeFolderLevel.Equals("icons", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Icon")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("illustrations", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Illustration")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("logos", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Logo")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("font", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Font")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Equals("social posts", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.SocialMedia")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Contains("social", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.SocialMedia")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Contains("email", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Email")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Contains("brochure", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Brochure")).FirstOrDefault().Id.Value,
                    _ when typeFolderLevel.Contains("webinar", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Webinar")).FirstOrDefault().Id.Value,
                    _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                };

                asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                return;
            }
        }
    }
}