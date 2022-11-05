using ContentHubConsole.Assets;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM
{
    public class DesignEmailAssetDetailer : BaseDetailer
    {
        public static readonly string UploadPath = @"C:\Users\ptjhi\Dropbox (Covetrus)\Consumer Creative\GPM\2022\Transactional";

        public DesignEmailAssetDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses) : base(webMClient, fileUploadResponses)
        {
        }

        public async override Task<long> UpdateAllAssets()
        {
            var results = new List<long>();

            await _taxonomyManager.LoadAllTaxonomies();
            long gpmStoreFrontBusinessDomainId = _taxonomyManager.BusinessDomainEntities.Where(w => w.Identifier.Equals("Business.Domain.GlobalPrescriptionManagement")).FirstOrDefault().Id.Value;
            long dropboxId = _taxonomyManager.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;
            long emailId = _taxonomyManager.AssetTypeEntities.Where(w => w.Identifier.Contains("Email")).FirstOrDefault().Id.Value;
            long emailUsageIds = _taxonomyManager.AssetUsageEntities.Where(w => w.Identifier.Contains("Email")).FirstOrDefault().Id.Value;

            foreach (var asset in _covetrusAsset)
            {
                try
                {
                    await asset.LoadAssetMembers();
                    asset.SetMigrationOriginPath(RemoveLocalPathPart(asset.OriginPath));
                    await asset.UpdateDescription(GetDescriptionFromOriginPath(asset.OriginPath));
                    asset.AddChildToManyParentsRelation(gpmStoreFrontBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                    asset.SetChildToOneParentRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                    asset.SetChildToOneParentRelation(emailId, RelationNames.RELATION_ASSETTYPE_TOASSET);
                    asset.AddChildToManyParentsRelation(emailUsageIds, RelationNames.RELATION_ASSETUSAGE_TOASSET);

                    SetSlices(asset);
                    SetStoreFrontBanners(asset);
                    SetBusinessToBusiness(asset);
                    SetStockImages(asset);
                    SetProductUsage(asset);
                    SetPromotion(asset);
                    SetEngagementEmail(asset);
                    SetTransactionalEmail(asset);
                    SetShippingEmail(asset);
                    SetReactivationEmail(asset);

                    SetYear(asset);
                    SetMonth(asset);
                    SetWeek(asset);

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

        private void SetWeek(CovetrusAsset asset)
        {
            long weekId = GetWeekIdFromPath(asset.OriginPath);

            if (weekId != 0)
            {
                asset.SetChildToOneParentRelation(weekId, CovetrusRelationNames.RELATION_WEEK_TOASSET);
            }
        }

        private void SetMonth(CovetrusAsset asset)
        {
            long monthId = GetMonthIdFromPath(asset.OriginPath);

            if (monthId != 0)
            {
                asset.SetChildToOneParentRelation(monthId, CovetrusRelationNames.RELATION_MONTH_TOASSET);
            }
        }

        private void SetYear(CovetrusAsset asset)
        {
            long yearId = GetYearIdFromPath(asset.OriginPath);

            if (yearId != 0)
            {
                asset.SetChildToOneParentRelation(yearId, CovetrusRelationNames.RELATION_YEAR_TOASSET);
            }
            
        }

        private void SetBusinessToBusiness(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("B2B"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("B2B"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetSlices(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Slices"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Slices"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetStoreFrontBanners(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Storefront"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("StoreFront")
                    || w.Identifier.Contains("Banner"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetStockImages(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Stock") || asset.OriginPath.Contains("Getty"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("StockImages"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetProductUsage(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Product") || asset.OriginPath.Contains("Kruuse"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Product"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetPromotion(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Promotion") || asset.OriginPath.Contains("Promo"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Promotion"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetTransactionalEmail(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Senior") || asset.OriginPath.Contains("Transactional"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Transactional"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetEngagementEmail(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Senior"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Engagement"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetReactivationEmail(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Reactivation"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Reactivation"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private void SetShippingEmail(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Shipping") || asset.OriginPath.Contains("Ship"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Shipping"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        private string RemoveLocalPathPart(string originPath)
        {
            var pathSplit = originPath.Split('\\').Skip(3).ToArray();
            var result = new StringBuilder();

            for (int i = 0; i < pathSplit.Length; i++)
            {
                result.Append($"\\{pathSplit[i]}");
            }

            return result.ToString();
        }

        private long GetWeekIdFromPath(string originPath)
        {
            var pathSplit = originPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("Week 01")))
            {
                var tax = _taxonomyManager.WeekEntities.Where(w => w.Identifier.Contains("01")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("Week 02")))
            {
                var tax = _taxonomyManager.WeekEntities.Where(w => w.Identifier.Contains("02")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("Week 03")))
            {
                var tax = _taxonomyManager.WeekEntities.Where(w => w.Identifier.Contains("03")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("Week 04")))
            {
                var tax = _taxonomyManager.WeekEntities.Where(w => w.Identifier.Contains("04")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("Week 05")))
            {
                var tax = _taxonomyManager.WeekEntities.Where(w => w.Identifier.Contains("05")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("Week 06")))
            {
                var tax = _taxonomyManager.WeekEntities.Where(w => w.Identifier.Contains("06")).FirstOrDefault();
                return tax.Id.Value;
            }

            return 0;
        }

        private long GetMonthIdFromPath(string originPath)
        {
            var pathSplit = originPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("January")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("January")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("February")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("February")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("March")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("March")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("April")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("April")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("May")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("May")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("June")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("June")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("July")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("July")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("August")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("August")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("September")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("September")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("October")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("October")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("November")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("November")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.EndsWith("December")))
            {
                var tax = _taxonomyManager.MonthEntities.Where(w => w.Identifier.Contains("December")).FirstOrDefault();
                return tax.Id.Value;
            }

            return 0;
        }

        private long GetYearIdFromPath(string originPath)
        {
            var pathSplit = originPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("2022")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2022")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("2023")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2023")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("2024")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2024")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("2025")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2025")).FirstOrDefault();
                return tax.Id.Value;
            }

            return 0;
        }
    }
}
