using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.VCP
{
    public class VcpAssetDetailer : BaseDetailer
    {
        public VcpAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            //long spPhotoBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.VeterinaryCarePlan")).FirstOrDefault().Id.Value;
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
                    //SetWebpage(asset);
                    SetUsages(asset);
                    //SetSeason(asset);
                    SetAdvertising(asset);
                    //await AddBrandFromPath(asset);
                    SetPractice(asset);
                    SetPracticeType(asset);

                    //SetYear(asset);
                    //SetSpecificYear(asset);
                    //SetMonth(asset);
                    //SetWeek(asset);

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

        private void SetPractice(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Clients-AAHA"))
            {
                long practiceTypeId = _taxonomyManager.PracticeEntities
                        .Where(w => w.Identifier.Equals("CV.Practice.PorterCountyAAHA"))
                        .Select(s => s.Id.Value).FirstOrDefault();

                if (practiceTypeId > 0)
                {
                    asset.AddChildToManyParentsRelation(practiceTypeId, CovetrusRelationNames.RELATION_PRACTICE_TOASSET);
                    return; 
                }
            }

            if (asset.OriginPath.Contains("Clients-Equine") 
                || asset.OriginPath.Contains("Clients-Independent") 
                || asset.OriginPath.Contains("Clients-Multi"))
            {
                var prac = asset.OriginPath.Split('\\').Skip(3).Take(1).FirstOrDefault();

                if (!String.IsNullOrEmpty(prac))
                {
                    var practId = $"CV.Practice.{prac.Replace("-", "")}";
                    long practiceTypeId = _taxonomyManager.PracticeEntities
                        .Where(w => w.Identifier.Equals(practId))
                        .Select(s => s.Id.Value).FirstOrDefault();

                    if (practiceTypeId> 0)
                    {
                        asset.AddChildToManyParentsRelation(practiceTypeId, CovetrusRelationNames.RELATION_PRACTICE_TOASSET);
                        return; 
                    }
                }
            }
        }

        private void SetPracticeType(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Clients-Equine"))
            {
                long practiceTypeId = _taxonomyManager.PracticeTypeEntities
                .Where(w => w.Identifier.Equals("CV.PracticeType.Equine"))
                .Select(s => s.Id.Value).FirstOrDefault();

                if (practiceTypeId > 0)
                {
                    asset.AddChildToManyParentsRelation(practiceTypeId, CovetrusRelationNames.RELATION_PRACTICETYPE_TOASSET);
                    return; 
                }
            }

            if (asset.OriginPath.Contains("Clients-Independent"))
            {
                long practiceTypeId = _taxonomyManager.PracticeTypeEntities
                .Where(w => w.Identifier.Equals("CV.PracticeType.Independent"))
                .Select(s => s.Id.Value).FirstOrDefault();

                if (practiceTypeId > 0)
                {
                    asset.AddChildToManyParentsRelation(practiceTypeId, CovetrusRelationNames.RELATION_PRACTICETYPE_TOASSET);
                    return;
                }
            }

            if (asset.OriginPath.Contains("Clients-Multi"))
            {
                long practiceTypeId = _taxonomyManager.PracticeTypeEntities
                .Where(w => w.Identifier.Equals("CV.PracticeType.Multiple"))
                .Select(s => s.Id.Value).FirstOrDefault();

                if (practiceTypeId > 0)
                {
                    asset.AddChildToManyParentsRelation(practiceTypeId, CovetrusRelationNames.RELATION_PRACTICETYPE_TOASSET);
                    return;
                }
            }
        }

        public override string RemoveLocalPathPart(string originPath)
        {
            var pathSplit = Program.IsVirtualMachine ? originPath.Split('\\').Skip(1).ToArray() : originPath.Split('\\').Skip(3).ToArray();
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
                var strSplit = s.Split('-');
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
            var typeCheck = asset.OriginPath.Split('\\').Skip(3).Take(1).FirstOrDefault();

            if (typeCheck != null)
            {
                var assetId = typeCheck switch
                {
                    _ when typeCheck.Equals("Fonts", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Font")).FirstOrDefault().Id.Value,
                    _ when typeCheck.Equals("Partners", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Veterinarian")).FirstOrDefault().Id.Value,
                    _ when typeCheck.Equals("PPT-Template", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Templates")).FirstOrDefault().Id.Value,
                    _ when typeCheck.Equals("Registration-Forms", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Forms")).FirstOrDefault().Id.Value,
                    _ when typeCheck.Contains("Stock", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.StockImages")).FirstOrDefault().Id.Value,
                    _ when typeCheck.Contains("Guidelines", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Guideline")).FirstOrDefault().Id.Value,
                    _ when typeCheck.Contains("Logos", StringComparison.InvariantCultureIgnoreCase) => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Logo")).FirstOrDefault().Id.Value,
                    _ => _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Equals("M.AssetType.Design")).FirstOrDefault().Id.Value
                };

                asset.SetChildToOneParentRelation(assetId, RelationNames.RELATION_ASSETTYPE_TOASSET);
            }
        }
    }
}