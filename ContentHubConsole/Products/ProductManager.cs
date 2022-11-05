using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ContentHubConsole.Products
{
    public class ProductManager
    {
        private readonly IWebMClient _webMClient;

        private const string PRODUCT_DEFINITION = "M.PCM.Product";
        private const string PRODUCT_FAMILY_DEFINITION = "M.PCM.ProductFamily";
        private const string PRODUCT_CATALOG_DEFINITION = "M.PCM.ProductCatalog";

        public ProductManager(IWebMClient webMClient)
        {
            _webMClient = webMClient;
        }

        public async Task<long> CreateProduct(string productName, string productNumber, string label, string shortDescription, string longDescription)
        {
            try
            {
                var culture = CultureInfo.CurrentCulture;

                IEntity asset = await _webMClient.EntityFactory.CreateAsync(PRODUCT_DEFINITION, CultureLoadOption.Default);
                asset.SetPropertyValue("ProductName", productName);
                asset.SetPropertyValue("ProductLabel", culture, label);
                asset.SetPropertyValue("ProductNumber", productNumber);
                asset.SetPropertyValue("ProductShortDescription", culture, shortDescription);
                asset.SetPropertyValue("ProductLongDescription", culture, longDescription);

                return await _webMClient.Entities.SaveAsync(asset);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                FileLogger.Log("CreateProduct", ex.Message);

                return 0;
            }
        }

        public async Task GetProducts()
        {
            try
            {
                var productEntities = await _webMClient.Entities.GetByDefinitionAsync(PRODUCT_DEFINITION, null, 0, 50);
                foreach (var product in productEntities.Items)
                {
                    await product.LoadMembersAsync(PropertyLoadOption.All, RelationLoadOption.All);
                    await product.LoadRelationsAsync(RelationLoadOption.All);
                    var assetRelation = product.GetRelation("PCMProductToMasterAsset");
                    var ids = assetRelation.GetIds();
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
