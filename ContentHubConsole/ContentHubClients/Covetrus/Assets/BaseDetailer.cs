using ContentHubConsole.Assets;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM;
using ContentHubConsole.ContentHubClients.Covetrus.Taxonomy;
using ContentHubConsole.Entities;
using ContentHubConsole.Products;
using ContentHubConsole.Taxonomies;
using Nito.AsyncEx;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Extensions;
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
        internal ProductManager _productManager;

        internal ICollection<CovetrusAsset> _covetrusAsset = new List<CovetrusAsset>();
        internal ICollection<CovetrusAsset> _failedAssets = new List<CovetrusAsset>();

        public static readonly string UploadPath = Program.OriginFolder;

        public BaseDetailer(IWebMClient webMClient, ICollection<FileUploadResponse> fileUploadResponses)
        {
            _webMClient = webMClient;
            _taxonomyManager = new CovetrusTaxonomyManager(webMClient);
            _productManager = new ProductManager(webMClient);
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

            foreach (var asset in _covetrusAsset.Where(w => w.Errors.Count() == 0))
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

        internal void SetWeek(CovetrusAsset asset)
        {
            long weekId = GetWeekIdFromPath(asset.OriginPath);

            if (weekId != 0)
            {
                asset.SetChildToOneParentRelation(weekId, CovetrusRelationNames.RELATION_WEEK_TOASSET);
            }
        }

        internal void SetMonth(CovetrusAsset asset)
        {
            long monthId = GetMonthIdFromPath(asset.OriginPath);

            if (monthId != 0)
            {
                asset.SetChildToOneParentRelation(monthId, CovetrusRelationNames.RELATION_MONTH_TOASSET);
            }
        }

        internal void SetYear(CovetrusAsset asset)
        {
            long yearId = GetYearIdFromPath(asset.OriginPath);

            if (yearId != 0)
            {
                asset.SetChildToOneParentRelation(yearId, CovetrusRelationNames.RELATION_YEAR_TOASSET);
            }
        }

        internal void SetSeason(CovetrusAsset asset)
        {
            long seasonId = GetSeasonIdFromPath(asset.OriginPath);

            if (seasonId != 0)
            {
                asset.AddChildToManyParentsRelation(seasonId, CovetrusRelationNames.RELATION_SEASON_TOASSET);
            }
        }

        internal async Task AssignToProduct(CovetrusAsset asset)
        {
            var filename = await asset.Asset.GetPropertyValueAsync<string>("Filename");

            if (filename.StartsWith("P") && filename.EndsWith(".pdf"))
            {
                var split = filename.Split('.');
                var productId = split[0].Replace("P", "").Substring(0,6);

                var products = await _productManager.GetProductByNumber(productId);

                if (products.Count > 0)
                {
                    foreach (var p in products)
                    {
                        asset.AddChildToManyParentsRelation(p.Id.Value, RelationNames.RELATION_PRODUCT_TOASSET);
                    }
                }
            }

            if (filename.StartsWith("V") && filename.EndsWith(".pdf"))
            {
                var split = filename.Split('.');
                var productId = split[0].Replace("V", "").Substring(0, 6);

                var products = await _productManager.GetProductByNumber(productId);

                if (products.Count > 0)
                {
                    foreach (var p in products)
                    {
                        asset.AddChildToManyParentsRelation(p.Id.Value, RelationNames.RELATION_PRODUCT_TOASSET);
                    }
                }
            }
        }

        internal async Task AssignToCatalogue(CovetrusAsset asset, string catalogName)
        {
            var catalolgs = await _productManager.GetCatalogs();
            var catalogId = catalolgs.Where(w => w.GetPropertyValue<string>("CatalogName") == catalogName).Select(s => s.Id.Value).FirstOrDefault();

            asset.AddChildToManyParentsRelation(catalogId, RelationNames.RELATION_CATALOG_TOASSET);
        }

        #region Set Asset Usages

        internal void SetBusinessToBusiness(CovetrusAsset asset)
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

        internal void SetSlices(CovetrusAsset asset)
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

        internal void SetStoreFrontBanners(CovetrusAsset asset)
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

        internal void SetStockImages(CovetrusAsset asset)
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

        internal void SetAdvertising(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("Advertising") || asset.OriginPath.Contains("Advertise"))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("Advertising"))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        internal void SetProductUsage(CovetrusAsset asset)
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

        internal void SetPromotion(CovetrusAsset asset)
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

        internal void SetTransactionalEmail(CovetrusAsset asset)
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

        internal void SetEngagementEmail(CovetrusAsset asset)
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

        internal void SetReactivationEmail(CovetrusAsset asset)
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

        internal void SetShippingEmail(CovetrusAsset asset)
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

        internal void SetOnsite(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("onsite", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("onsite", StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        internal void SetOffsite(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("offsite", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("offsite", StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        internal void SetWebpage(CovetrusAsset asset)
        {
            if (asset.OriginPath.Contains("homepage", StringComparison.InvariantCultureIgnoreCase))
            {
                List<long> usageIds = _taxonomyManager.AssetUsageEntities
                .Where(w => w.Identifier.Contains("webpage", StringComparison.InvariantCultureIgnoreCase))
                .Select(s => s.Id.Value).ToList();

                foreach (var usage in usageIds)
                {
                    asset.AddChildToManyParentsRelation(usage, RelationNames.RELATION_ASSETUSAGE_TOASSET);
                }
            }
        }

        

        #endregion

        #region Helpers

        public virtual string RemoveLocalPathPart(string originPath)
        {
            var pathSplit = Program.IsVirtualMachine ? originPath.Split('\\').Skip(1).ToArray() : originPath.Split('\\').Skip(3).ToArray();
            var result = new StringBuilder();

            for (int i = 0; i < pathSplit.Length; i++)
            {
                result.Append($"\\{pathSplit[i]}");
            }

            return result.ToString();
        }

        public virtual long GetWeekIdFromPath(string originPath)
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

        public virtual long GetMonthIdFromPath(string originPath)
        {
            var pathSplit = originPath.Split('\\');
            if (pathSplit.Any(a => a.EndsWith("January")))
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

        public virtual long GetYearIdFromPath(string originPath)
        {
            var pathSplit = originPath.Split('\\');
            if (pathSplit.Any(a => a.Equals("2019")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2019")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("2020")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2020")).FirstOrDefault();
                return tax.Id.Value;
            }

            if (pathSplit.Any(a => a.Equals("2021")))
            {
                var tax = _taxonomyManager.YearEntities.Where(w => w.Identifier.Contains("2021")).FirstOrDefault();
                return tax.Id.Value;
            }

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

        public virtual long GetSeasonIdFromPath(string originPath)
        {
            var pathSplit = originPath.Split('\\');
            if (pathSplit.Any(a => a.Contains("winter", StringComparison.InvariantCultureIgnoreCase)))
            {
                var season = _taxonomyManager.SeasonEntities.Where(w => w.Identifier.Contains("winter", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                return season.Id.Value;
            }

            if (pathSplit.Any(a => a.Contains("spring", StringComparison.InvariantCultureIgnoreCase)))
            {
                var season = _taxonomyManager.SeasonEntities.Where(w => w.Identifier.Contains("spring", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                return season.Id.Value;
            }

            if (pathSplit.Any(a => a.Contains("summer", StringComparison.InvariantCultureIgnoreCase)))
            {
                var season = _taxonomyManager.SeasonEntities.Where(w => w.Identifier.Contains("summer", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                return season.Id.Value;
            }

            if (pathSplit.Any(a => a.Contains("fall", StringComparison.InvariantCultureIgnoreCase)) || pathSplit.Any(a => a.Contains("F22", StringComparison.InvariantCultureIgnoreCase)))
            {
                var season = _taxonomyManager.SeasonEntities.Where(w => w.Identifier.Contains("fall", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                return season.Id.Value;
            }

            if (pathSplit.Any(a => a.Contains("holiday", StringComparison.InvariantCultureIgnoreCase)))
            {
                var season = _taxonomyManager.SeasonEntities.Where(w => w.Identifier.Contains("holiday", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
                return season.Id.Value;
            }

            return 0;
        }


        #endregion
    }
}
