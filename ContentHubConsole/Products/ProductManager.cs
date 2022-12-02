using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
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

        public async Task<List<IEntity>> GetProducts(int skip = 0, int take = 3000)
        {
            try
            {
                var productEntities = await _webMClient.Entities.GetByDefinitionAsync(PRODUCT_DEFINITION, null, skip, take);
                foreach (var product in productEntities.Items)
                {
                    await product.LoadMembersAsync(PropertyLoadOption.All, RelationLoadOption.All);
                    await product.LoadRelationsAsync(RelationLoadOption.All);
                    //var assetRelation = product.GetRelation("PCMProductToMasterAsset");
                    //var ids = assetRelation.GetIds();
                }

                return productEntities.Items.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                FileLogger.Log("GetProducts", ex.Message);

                return new List<IEntity>();
            }
        }

        public async Task<List<IEntity>> GetProductByNumber(string productNumber)
        {
            try
            {
                Query query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.DefinitionName == PRODUCT_DEFINITION
                    && e.Property("ProductNumber") == productNumber
                  select e).Skip(0).Take(100));

                var prods = await _webMClient.Querying.QueryAsync(query);
                return prods.Items.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                FileLogger.Log("GetProductByNumber", ex.Message);

                return new List<IEntity>();
            }
        }
    }
}
