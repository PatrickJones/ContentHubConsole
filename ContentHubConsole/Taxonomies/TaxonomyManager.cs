using Nito.AsyncEx;
using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.Contracts.Querying;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.Taxonomies
{
    public class TaxonomyManager
    {
        internal readonly IWebMClient _webMClient;

        public readonly string M_ASSET_TYPE = "M.AssetType";
        public readonly string M_PCM_CATALOG = "M.PCM.Catalog";
        public readonly string M_PCM_PRODUCT_CATEGORY = "M.PCM.ProductCategory";
        public readonly string M_PCM_PRODUCT_FAMILY = "M.PCM.ProductFamily";
        public readonly string M_PCM_PRODUCT_STATUS = "M.PCM.ProductStatus";
        public readonly string M_TAG = "M.Tag";
        public readonly string M_BRAND = "M.Brand";

        public ICollection<IEntity> AssetTypeEntities = new List<IEntity>();
        public ICollection<IEntity> CatalogEntities = new List<IEntity>();
        public ICollection<IEntity> ProductCategoryEntities = new List<IEntity>();
        public ICollection<IEntity> ProductFamilyEntities = new List<IEntity>();
        public ICollection<IEntity> ProductStatusEntities = new List<IEntity>();

        public TaxonomyManager(IWebMClient webMClient)
        {
            _webMClient = webMClient;
        }

        public async Task LoadAllDefaultTaxonomies()
        {
            var tasks = new List<Task>();

            tasks.Add(LoadAssetTypes());
            tasks.Add(LoadProductCatalogs());
            tasks.Add(LoadProductCategories());
            tasks.Add(LoadProductFamilies());
            tasks.Add(LoadProductStatus());

            await tasks.WhenAll();
        }

        public async Task<IEntityQueryResult> GetTaxonomyByDefinitionQueyable(string definitionName, int skip = 0, int take = 100)
        {
            return await _webMClient.Entities.GetByDefinitionAsync(definitionName, null, skip, take);
        }

        public async Task<ICollection<IEntity>> GetTaxonomyByDefinition(string definitionName, int skip = 0, int take = 100)
        {
            var entities = await _webMClient.Entities.GetByDefinitionAsync(definitionName, null, skip, take);
            return entities.Items.ToList();
        }

        public async Task<long> AddTagValue(string tagValue)
        {
            var tagLower = tagValue.ToLower();
            var tagValueTrimmed = CondenseValue(tagLower);
            try
            {
                var query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.DefinitionName == M_TAG && e.Property("TagName") == tagLower
                  select e).Skip(0).Take(100));
                var mq = await _webMClient.Querying.QueryAsync(query);
                

                if (mq.Items.Any())
                {
                    var tags = mq.Items.ToList();
                    return tags.Select(s => s.Id.Value).FirstOrDefault();
                }

                if (!String.IsNullOrEmpty(tagValueTrimmed))
                {
                    var tagEntity = await _webMClient.EntityFactory.CreateAsync(M_TAG);
                    tagEntity.Identifier = $"{M_TAG}.{tagValueTrimmed}";
                    tagEntity.SetPropertyValue("TagName", tagLower);
                    tagEntity.SetPropertyValue("TagLabel", CultureInfo.CurrentCulture, tagLower);

                    return await _webMClient.Entities.SaveAsync(tagEntity);
                }
            }
            catch (Exception ex)
            {
                var error = $"Error adding new M.Tag: {tagLower}";
                Console.WriteLine(error);
                FileLogger.Log("AddTagValue", error);
            }

            return 0;
        }

        public async Task<long> AddBrandValue(string brandValue)
        {
            var tagLower = brandValue;
            var tagValueTrimmed = CondenseValue(tagLower, false);
            try
            {
                var query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.DefinitionName == M_BRAND && e.Property("BrandName") == tagLower
                  select e).Skip(0).Take(100));
                var mq = await _webMClient.Querying.QueryAsync(query);


                if (mq.Items.Any())
                {
                    var tags = mq.Items.ToList();
                    return tags.Select(s => s.Id.Value).FirstOrDefault();
                }

                if (!String.IsNullOrEmpty(tagValueTrimmed))
                {
                    var tagEntity = await _webMClient.EntityFactory.CreateAsync(M_BRAND);
                    tagEntity.Identifier = $"{M_BRAND}.{tagValueTrimmed}";
                    tagEntity.SetPropertyValue("BrandName", tagLower);
                    tagEntity.SetPropertyValue("BrandSynonyms", CultureInfo.CurrentCulture, tagLower);
                    tagEntity.SetPropertyValue("BrandDescription", CultureInfo.CurrentCulture, tagLower);
                    tagEntity.SetPropertyValue("BrandLabel", CultureInfo.CurrentCulture, tagLower);

                    return await _webMClient.Entities.SaveAsync(tagEntity);
                }
            }
            catch (Exception ex)
            {
                var error = $"Error adding new M.Brand: {tagLower}";
                Console.WriteLine(error);
                FileLogger.Log("AddBrandValue", error);
            }

            return 0;
        }


        internal string CondenseValue(string tagValue, bool allLower = true)
        {
            var split = tagValue.Trim().Split(' ').ToArray();
            var result = String.Empty;
            foreach (var item in split)
            {
                result = result + item;
            }

            return allLower ? result.ToLower() : result;
        }

        private async Task LoadProductStatus(int skip = 0, int take = 100)
        {
            if (!ProductStatusEntities.Any())
            {
                var productStatus = await _webMClient.Entities.GetByDefinitionAsync(M_PCM_PRODUCT_STATUS, null, skip, take);
                ProductStatusEntities = productStatus.Items.ToList();
            }
        }

        private async Task LoadProductFamilies(int skip = 0, int take = 100)
        {
            if (!ProductFamilyEntities.Any())
            {
                var productFamilies = await _webMClient.Entities.GetByDefinitionAsync(M_PCM_PRODUCT_FAMILY, null, skip, take);
                ProductFamilyEntities = productFamilies.Items.ToList();
            }
        }

        private async Task LoadProductCategories(int skip = 0, int take = 2000)
        {
            if (!ProductCategoryEntities.Any())
            {
                var productCategories = await _webMClient.Entities.GetByDefinitionAsync(M_PCM_PRODUCT_CATEGORY, null, skip, take);
                ProductCategoryEntities = productCategories.Items.ToList();
            }
        }

        private async Task LoadProductCatalogs(int skip = 0, int take = 100)
        {
            if (!CatalogEntities.Any())
            {
                var catalogs = await _webMClient.Entities.GetByDefinitionAsync(M_PCM_CATALOG, null, skip, take);
                CatalogEntities = catalogs.Items.ToList();
            }
        }

        private async Task LoadAssetTypes(int skip = 0, int take = 100)
        {
            if (!AssetTypeEntities.Any())
            {
                var assetTypes = await _webMClient.Entities.GetByDefinitionAsync(M_ASSET_TYPE, null, skip, take);
                AssetTypeEntities = assetTypes.Items.ToList();
            }
        }
    }
}
