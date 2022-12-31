using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.Brand
{
    internal class BrandBasicAssetDetailer : BaseDetailer
    {
        public BrandBasicAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            //long spPhotoBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.Brand")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long imageId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value;
            long imageUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Equals("CV.AssetUsage.Design")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    //asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    //asset.AddChildToManyParentsRelation(spPhotoBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(imageId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(imageUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    await AddTagFromPath(asset);

                    SetStockImages(asset);
                    //SetProductUsage(asset);
                    //SetOffsite(asset);
                    //SetOnsite(asset);
                    SetWebpage(asset);
                    SetUsages(asset);
                    SetSeason(asset);
                    //SetAdvertising(asset);
                    //await AddBrandFromPath(asset);

                    SetYear(asset);
                    //SetSpecificYear(asset);
                    SetMonth(asset);
                    SetWeek(asset);

                    UpdateAssetType(asset);
                    
                    await asset.SaveAsset();
                    ActuallySaved++;

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
            var pathStrings = new List<string>();
            var orgSpilt = asset.OriginPath.Split("\\").ToList();

            foreach (var s in orgSpilt)
            {
                var strSplit = s.Split(' ');
                pathStrings.AddRange(strSplit);
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
            var pathSplit = asset.OriginPath.Split('\\');
            long typeId = 0;

            if (typeId == 0 && String.IsNullOrEmpty(pathSplit[4]))
            {
                string typeCheck = pathSplit[4];

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Contains("guides", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.HowToGuides")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("icons", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Icon")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("logos", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Logo")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("Illustrations", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Illustration")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("fonts", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Font")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("video", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Videos")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    return;
                }
            }

            if (typeId == 0)
            {
                string typeCheck = pathSplit.Last();

                if (typeCheck != null)
                {
                    var assetId = typeCheck switch
                    {
                        _ when typeCheck.Contains("guides", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.HowToGuides")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("icons", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Icon")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("logos", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Logo")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("Illustrations", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Illustration")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("fonts", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Font")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("video", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Videos")).FirstOrDefault().Id.Value,
                        _ when typeCheck.Contains("label", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Label")).FirstOrDefault().Id.Value,
                        _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                    };

                    asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    return;
                }
            }
        }
    }
}