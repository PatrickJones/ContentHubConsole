using ContentHubConsole.Assets;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.CONAEcomm;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.SmartPak;
using Stylelabs.M.Base.Querying;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using Stylelabs.M.Sdk.Contracts.Base;
using Stylelabs.M.Sdk.WebClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
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
        private const string PRODUCT_CATALOG_DEFINITION = "M.PCM.Catalog";

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

        public async Task<List<IEntity>> GetCatalogs(int skip = 0, int take = 100)
        {
            try
            {
                var catalogEntities = await _webMClient.Entities.GetByDefinitionAsync(PRODUCT_CATALOG_DEFINITION, null, skip, take);
                foreach (var catalog in catalogEntities.Items)
                {
                    await catalog.LoadMembersAsync(PropertyLoadOption.All, RelationLoadOption.All);
                    await catalog.LoadRelationsAsync(RelationLoadOption.All);
                    //var assetRelation = product.GetRelation("PCMProductToMasterAsset");
                    //var ids = assetRelation.GetIds();
                }

                return catalogEntities.Items.ToList();
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

        public async Task<List<FileUploadResponse>> GetMigratedAssetsWithAssignedProducts()
        {
            var fileUploadResponses = new List<FileUploadResponse>();
            var dateMin = new DateTime(2022, 12, 1);
            int skip = 0;
            var take = 5000;

            try
            {
                Query query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.DefinitionName == "M.Asset"
                    && e.Parent("PCMProductToAsset") == null
                    && e.Parent("BusinessDomainToAsset").In(32586)
                    && e.ModifiedByUsername == "patrick.jones@xcentium.com"
                    && (e.Property("Title").StartsWith("P0") || e.Property("Title").StartsWith("V0"))
                    && e.Property("OriginPath").Contains("MASTER FILES")
                    && e.ModifiedOn > dateMin
                  select e).Skip(skip).Take(take));

                var mq = await _webMClient.Querying.QueryAsync(query);

                if (mq.Items.Any())
                {
                    Console.WriteLine($"Assets found without product relation: {mq.Items.Count}");
                    var tags = mq.Items.ToList();
                    var assets = tags.Select(s => new { AsssetId = s.Id.Value, Filename = s.GetPropertyValue<string>("FileName") }).ToList();
                    //return (assets.AsssetId, assets.Filename);


                    foreach (var asset in assets)
                    {
                        try
                        {
                            var files = Directory.GetFiles(ConaEcommMSDSAssetDetailer.UploadPath, asset.Filename, SearchOption.AllDirectories).Distinct();
                            if (files.Any() && files.Count() == 1)
                            {
                                fileUploadResponses.Add(new FileUploadResponse(asset.AsssetId, files.FirstOrDefault()));
                            }

                            if (files.Any() && files.Count() > 1)
                            {
                                var filtered = files.Except(fileUploadResponses.Select(s => s.LocalPath).ToList());
                                fileUploadResponses.Add(new FileUploadResponse(asset.AsssetId, filtered.FirstOrDefault()));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting path for asset id: {asset.AsssetId}. Messsage: {ex.Message}");
                            continue;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No assets found.");
                }

                //if (!String.IsNullOrEmpty(tagValueTrimmed))
                //{
                //    var tagEntity = await _webMClient.EntityFactory.CreateAsync(M_TAG);
                //    tagEntity.Identifier = $"{M_TAG}.{tagValueTrimmed}";
                //    tagEntity.SetPropertyValue("TagName", tagLower);
                //    tagEntity.SetPropertyValue("TagLabel", CultureInfo.CurrentCulture, tagLower);

                //    return await _webMClient.Entities.SaveAsync(tagEntity);
                //}
            }
            catch (Exception ex)
            {
                var error = $"Error assets";
                Console.WriteLine(error);
                FileLogger.Log("AddTagValue", error);
            }
            Console.WriteLine($"Assets getting updated: {fileUploadResponses.Count}");
            return fileUploadResponses;
        }

        public async Task<List<FileUploadResponse>> GetMigratedAssetsWithAssignedCatalog()
        {
            var fileUploadResponses = new List<FileUploadResponse>();
            var dateMin = new DateTime(2022, 12, 1);
            int skip = 0;
            var take = 500;

            try
            {
                Query query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.DefinitionName == "M.Asset"
                    && e.Parent("PCMCatalogToAsset") == null
                    && e.Parent("BusinessDomainToAsset").In(32586)
                    && e.ModifiedByUsername == "patrick.jones@xcentium.com"
                    && e.Property("OriginPath").Contains("MASTER FILES")
                    && e.Property("OriginPath").Contains("PDFs")
                    && e.ModifiedOn > dateMin
                  select e).Skip(skip).Take(take));

                var mq = await _webMClient.Querying.QueryAsync(query);

                if (mq.Items.Any())
                {
                    Console.WriteLine($"Assets found without product relation: {mq.Items.Count}");
                    var tags = mq.Items.ToList();
                    var assets = tags.Select(s => new { AsssetId = s.Id.Value, Filename = s.GetPropertyValue<string>("FileName") }).ToList();
                    //return (assets.AsssetId, assets.Filename);


                    foreach (var asset in assets)
                    {
                        try
                        {
                            var files = Directory.GetFiles(ConaEcommMSDSAssetDetailer.UploadPath, asset.Filename, SearchOption.AllDirectories).Distinct();
                            if (files.Any() && files.Count() == 1)
                            {
                                fileUploadResponses.Add(new FileUploadResponse(asset.AsssetId, files.FirstOrDefault()));
                            }

                            if (files.Any() && files.Count() > 1)
                            {
                                var filtered = files.Except(fileUploadResponses.Select(s => s.LocalPath).ToList());
                                fileUploadResponses.Add(new FileUploadResponse(asset.AsssetId, filtered.FirstOrDefault()));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error getting path for asset id: {asset.AsssetId}. Messsage: {ex.Message}");
                            continue;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No assets found.");
                }

                //if (!String.IsNullOrEmpty(tagValueTrimmed))
                //{
                //    var tagEntity = await _webMClient.EntityFactory.CreateAsync(M_TAG);
                //    tagEntity.Identifier = $"{M_TAG}.{tagValueTrimmed}";
                //    tagEntity.SetPropertyValue("TagName", tagLower);
                //    tagEntity.SetPropertyValue("TagLabel", CultureInfo.CurrentCulture, tagLower);

                //    return await _webMClient.Entities.SaveAsync(tagEntity);
                //}
            }
            catch (Exception ex)
            {
                var error = $"Error assets";
                Console.WriteLine(error);
                FileLogger.Log("AddTagValue", error);
            }
            Console.WriteLine($"Assets getting updated: {fileUploadResponses.Count}");
            return fileUploadResponses;
        }

    }
}
