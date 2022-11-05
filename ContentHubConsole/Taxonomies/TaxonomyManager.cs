using Nito.AsyncEx;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.Contracts.Querying;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
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

        private async Task LoadProductCategories(int skip = 0, int take = 100)
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
