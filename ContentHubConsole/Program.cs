using System;
using Stylelabs.M.Sdk;
using static System.Net.HttpStatusCode;
using static Stylelabs.M.Sdk.Clients.IEntitiesClient;
//using Stylelabs.M.Scripting.Types;
//using Stylelabs.M.Framework.LoadConfigurations;
//using Stylelabs.M.Framework.LoadOptions;
using Stylelabs.M.Base.Querying.Linq;
using Stylelabs.M.Base.Querying.Filters;
using static Stylelabs.M.Base.Querying.Query;
using static Stylelabs.M.Base.Querying.ScrollRequest;
using static Stylelabs.M.Base.Querying.Sorting;
using Newtonsoft;
using Stylelabs.M.Sdk.Contracts.Base;

// Cannot use these namespaces in script
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stylelabs.M.Sdk.WebClient.Authentication;
using Stylelabs.M.Sdk.WebClient;

using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Binder;
using Stylelabs.M.Sdk.Exceptions;
using Stylelabs.M.Framework.Essentials.LoadOptions;
using ContentHubConsole.Products;
using ContentHubConsole.Assets;
using ContentHubConsole.Entities;
using System.Collections.Generic;
using ContentHubConsole.Taxonomies;
using ContentHubConsole.ContentHubClients.Covetrus.Taxonomy;
using System.Linq;
using ContentHubConsole.ContentHubClients.Covetrus.Assets;
using ContentHubConsole.ContentHubClients;
using System.IO;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.GPM;
using System.Globalization;
using Microsoft.Extensions.Azure;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;
using Stylelabs.M.Base.Querying;
using static Stylelabs.M.Sdk.Constants;
using System.ServiceProcess;
using System.Reflection;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.SmartPak;
using static Stylelabs.M.Sdk.Errors;
using System.Net;
using Stylelabs.M.Sdk.Models.Base;
using static Stylelabs.M.Sdk.WebClient.WebClientErrors;
using ContentHubConsole.LogicApps;
using System.Collections.Concurrent;
using Nito.AsyncEx;
using Azure.Core;
using System.Text.Json;
using ContentHubConsole.AzureFunctions;
using static System.Net.WebRequestMethods;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.CONAEcomm;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.CONACC;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.VCP;
using ContentHubConsole.ContentHubClients.Covetrus.Assets.Brand;

namespace ContentHubConsole
{
    class Program
    {
        private static bool _useSandbox = false;
        static IConfiguration Configuration { get; set; }
        static IConfigurationRefresher _refresher;
        private static long _contenthubMaxFileSize = 1048576;

        private static string _contentHubToken = String.Empty;
        private static string _serviceName = String.Empty;

        public static string FileLoggerLocation = String.Empty;
        public static string DropboxUrl = String.Empty;
        public static string DropboxSingleFileUrl = String.Empty;
        public static string LargeFileFunctionUrl = String.Empty;
        public static string OriginFolder = String.Empty;
        public static bool TestMode = false;
        public static int TestModeTake = 1;
        public static bool IsVirtualMachine = false;

        public static List<FileInfo> LogicAppFiles = new List<FileInfo>();
        public static List<FileInfo> FunctionAppFiles = new List<FileInfo>();
        public static List<FileInfo> ProgramAppFiles = new List<FileInfo>();

        static async Task Main(string[] args)
        {
            //RunAsAService();

            string appconfigConnection = Environment.GetEnvironmentVariable("ContentHubAppConfig");

            var builder = new ConfigurationBuilder();
            builder.AddJsonFile("appsettings.json");
            builder.AddAzureAppConfiguration(options =>
            {
                options.Connect(appconfigConnection)
                       .Select(keyFilter: "ContentHub:*", labelFilter: "CovetrusClient");
                //.ConfigureRefresh(refresh =>
                //{
                //    refresh.Register("AppName")
                //           .Register("RedirectUrl", "PJWebClient" refreshAll: true)
                //           .SetCacheExpiration(TimeSpan.FromSeconds(10));
                //});

                // Get an instance of the refresher that can be used to refresh data
                //_refresher = options.GetRefresher();
            });

            Configuration = builder.Build();
            IsVirtualMachine = Boolean.Parse(Configuration["IsVirtualMachine"]);
            TestMode = Boolean.Parse(Configuration["TestMode"]);
            TestModeTake = Int32.Parse(Configuration["TestModeTake"]);
            OriginFolder = IsVirtualMachine ? Configuration["OriginFolderVM"] : Configuration["OriginFolder"];
            DropboxUrl = Configuration["LogicApps:0:DropboxUrl"];
            DropboxSingleFileUrl = Configuration["LogicApps:0:DropboxSingleFileUrl"];
            LargeFileFunctionUrl = Configuration["AzureFunctions:0:LargeFileFunctionUrl"];
            FileLoggerLocation = IsVirtualMachine ? Configuration["FileLoggerPathVM"] : Configuration["FileLoggerPath"];
            _contentHubToken = Configuration["ContentHubToken"];
            _serviceName = Configuration["ServiceName"];

            CreateLogFile();
            SetAppFileCollections();

            if (_useSandbox)
            {
                var conf = Configuration["Sandboxes:0:Covetrus"] ?? "Content hub url not found";
                Console.WriteLine(conf);
                FileLogger.Log("Program", conf);

                var sandoxRunning = await IsSandboxRunning(Configuration["Sandboxes:0:Covetrus"]);
                if (!sandoxRunning)
                {
                    var ss = "Sandbox is not running.";
                    Console.WriteLine(ss);
                    FileLogger.Log("Program", ss);

                    return;
                }
            }

            try
            {
                var clientFactory = new ContentHubClientFactory(Configuration["ContentHub:ClientId"],
                    Configuration["ContentHub:ClientSecret"],
                    Configuration["ContentHub:Username"],
                    Configuration["ContentHub:Password"],
                    Configuration["ContentHub:RedirectUrl"]
                );

                await clientFactory.Client().TestConnectionAsync();
                Console.WriteLine("Connection is successful.");
                FileLogger.Log("Program", "Connection is successful.");

                var mClient = clientFactory.Client();
                List<Task> tasks = new List<Task>();

                //await GetTotalMigratedFromPath(mClient);
                //tasks.Add(DefaultExecution(mClient));
                //tasks.Add(MissingFileExecution(mClient));
                //await MissingFileExecutionUsingLogicApp(mClient);
                //await MigratedAssetsWithNoTypeExecution(mClient, true);
                //await ReloadAssetsWithZeroFileSizeExecution(mClient);
                //await ReloadModifiedAssetsExecution(mClient);
                ////await MigratedAssetsWithNoAssignedProduct(mClient);
                //await MigratedAssetsWithNoAssignedCatalog(mClient);


                //await tasks.WhenAll();
                await MigratedAssetsWithNoTypeExecution(mClient, true);



                //################## 

                //var em = new EntityManager(mClient);
                //var mig = await em.GetMigratedAssetsWithNoType();
                //await em.RemoveAssets();
                //##################


                //var em = new ProductManager(mClient);
                //var mig = await em.GetCatalogs();
                //foreach (var cat in mig)
                //{
                //    Console.WriteLine(cat.Id);
                //}
                //##################


                //##################

                //try
                //{
                //    var tax = new CovetrusTaxonomyManager(mClient);
                //    await tax.LoadAllDefaultTaxonomies();
                //    var list = tax.ProductCategoryEntities.Where(w => w.Identifier != "M.PCM.ProductCategory.CONA"
                //    || w.Identifier != "M.PCM.ProductCategory.GPMSmartPak" || w.Identifier != "M.PCM.ProductCategory.GPMSmartPak.Accessories");
                //    foreach (var item in list)
                //    {
                //        try
                //        {
                //            await mClient.Entities.DeleteAsync(item.Id.Value);
                //        }
                //        catch (Exception e)
                //        {
                //            Console.WriteLine($"Looping error: {e.Message}");
                //            continue;
                //        }

                //    }
                //}
                //catch (Exception ex)
                //{
                //    Console.WriteLine(ex.Message);
                //}

                //var taxId = await tax.AddTagValue("safari lion");
                //var i = 0;

                //var sharedBusinessDomainId = tax.BusinessDomainEntities.Where(w => w.Identifier.Contains("Shared")).FirstOrDefault().Id;

                //Console.WriteLine($"BusinessDomains count: {tax.BusinessDomainEntities.Count}");

                //##################

                //var directoryPath = @"C:\Users\ptjhi\Dropbox (Covetrus)\Product Images\Manufacturer Originals\Akorn";
                //var uploadMgr = new UploadManager(mClient);
                //var uploads = await uploadMgr.UploadLocalDirectory(directoryPath);

                //foreach (var upload in uploads)
                //{
                //    long gpmStoreFrontBusinessDomainId = tax.BusinessDomainEntities.Where(w => w.Identifier.Contains("StoreFront")).FirstOrDefault().Id.Value;
                //    long dropboxId = tax.MigrationOriginEntities.Where(w => w.Identifier.Contains("Dropbox")).FirstOrDefault().Id.Value;

                //    var asset = new CovetrusAsset(mClient, upload);
                //    await asset.LoadAssetMembers();
                //    await asset.UpdateDescription("some description");
                //    asset.AddChildToManyParentsRelation(gpmStoreFrontBusinessDomainId, CovetrusRelationNames.RELATION_BUSINESSDOMAIN_TOASSET);
                //    asset.SetRelation(dropboxId, CovetrusRelationNames.RELATION_MIGRATIONORIGIN_TOASSET);
                //    var saved = await asset.SaveAsset();

                //    Console.WriteLine($"New asset {upload.AssetId} from path {upload.LocalPath}");
                //}

                //##################

                //var productMgr = new ProductManager(mClient);
                //var newProductId = await productMgr.CreateProduct("My prod name", "12345-ccc", "My Product", "My test product", DescriptionBuilder.BuildBulletString("My", "test", "Product"));
                //Console.WriteLine($"New product entity created: {newProductId}");

                //##################



                //##################
                //var taxUsage = await mClient.Entities.GetByDefinitionAsync("M.AssetType");

                //foreach (var item in taxUsage.Items.Select(s => s.Identifier))
                //{
                //    var name = item.Split('.').Last();

                //    var asset = await mClient.EntityFactory.CreateAsync("CV.AssetUsage");
                //    asset.Identifier = $"CV.AssetUsage.{name}";
                //    asset.SetPropertyValue("TaxonomyName", name);
                //    asset.SetPropertyValue("TaxonomyLabel", CultureInfo.CurrentCulture, name);
                //    var id = await mClient.Entities.SaveAsync(asset);
                //    Console.WriteLine(id);
                //}
            }
            catch (NotFoundException ex)
            {
                var nf_ex = $"Connection is NOT successful. Message: {ex.Message}";
                Console.WriteLine(nf_ex);
                FileLogger.Log("NotFoundException", nf_ex);
            }
            catch (ScriptException ex)
            {
                var nf_ex = $"Connection is NOT successful. Message: {ex.Message}";
                Console.WriteLine(nf_ex);
                FileLogger.Log("ScriptException", nf_ex);
            }
            catch (Exception ex)
            {
                var nf_ex = $"Connection is NOT successful. Message: {ex.Message}";
                Console.WriteLine(nf_ex);
                FileLogger.Log("Exception", nf_ex);
            }

            //StopWindowsService();

        }

        private static void SetAppFileCollections()
        {
            var files = Directory.GetFiles(OriginFolder, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (var file in files)
            {
                FileInfo fi = new FileInfo(file);
                if (fi.Length > 0 && fi.Length < DropboxLogicApp.MaxFileSize)
                {
                    LogicAppFiles.Add(fi);
                }
                else if (fi.Length >= DropboxLogicApp.MaxFileSize && fi.Length < 3221225472) //3GB
                {
                    FunctionAppFiles.Add(fi);
                }
                else
                {
                    ProgramAppFiles.Add(fi);
                }
            }
        }

        private static string PathTrimmer(string path, int skip, char delimeter = '\\')
        {
            var pathList = path.Split(delimeter).Skip(skip).ToList();
            var pathPart = String.Empty;
            foreach (var item in pathList)
            {
                pathPart = $@"{pathPart}{delimeter}{item}";
            }

            return pathPart;
        }

        private static string PathTrimmerLogicApp(string path, int skip, char delimeter = '\\')
        {
            var pathList = path.Split(delimeter).Skip(skip).ToList();
            var pathPart = String.Empty;
            foreach (var item in pathList)
            {
                pathPart = $@"{pathPart}/{item}";
            }

            return pathPart;
        }

        private static async Task<long> GetTotalMigratedFromPath(IWebMClient mClient)
        {
            var pathPart = PathTrimmer(OriginFolder, 4);
            
            Console.WriteLine($"Getting Total Migrated for path part: {pathPart}");

            var query = Query.CreateQuery(entities =>
             (from e in entities
              where e.DefinitionName == "M.Asset"
                    && e.ModifiedByUsername == "patrick.jones@xcentium.com"
                    && e.Property("OriginPath").Contains(pathPart)
              select e).Skip(0).Take(5000));
            var mq = await mClient.Querying.QueryAsync(query);

            Console.WriteLine($"Total Migrated: {mq.Items.ToList().LongCount()}");
            return mq.Items.ToList().LongCount();
        }

        public static async Task DefaultExecution(IWebMClient mClient)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);
            var directoryPath = ConaEcommProductAssetDetailer.UploadPath;
            await uploadMgr.UploadLocalDirectory(directoryPath, SearchOption.AllDirectories);
            await uploadMgr.UploadLargeFileLocalDirectory();

            var gpm = new ConaEcommProductAssetDetailer(mClient, uploadMgr.DirectoryFileUploadResponses);
            await gpm.UpdateAllAssets();
            //await gpm.SaveAllAssets();

            //if (gpm._failedAssets.Any())
            //{
            //    FileLogger.Log("Program.DefaultExecution", $"Failed Assets:");
            //    ICollection<FileUploadResponse> failedFiles = new List<FileUploadResponse>();
            //    foreach (var ff in gpm._failedAssets)
            //    {
            //        var uploadFailedFile = await uploadMgr.UploadLocalFile(ff.OriginPath);
            //        failedFiles.Add(uploadFailedFile);
            //    }

            //    var gpmRetry = new ConaEcommProductAssetDetailer(mClient, failedFiles);
            //    await gpmRetry.UpdateAllAssets();
            //    await gpmRetry.SaveAllAssets();

            //    var ffc = $"Failed files count: {gpmRetry._failedAssets.Count}";
            //    Console.WriteLine(ffc);
            //    FileLogger.Log("Program.DefaultExecution", ffc);

            //    foreach (var failed in gpmRetry._failedAssets)
            //    {
            //        var fp = $"{failed.OriginPath}";
            //        Console.WriteLine(fp);
            //        FileLogger.Log("Program.DefaultExecution", fp);
            //        FileLogger.AddToFailedUploadLog(failed.OriginPath);
            //    }
            //}

            Console.WriteLine($"Completed {gpm.ActuallySaved}");
            FileLogger.Log("Program", $"Completed {gpm.ActuallySaved}");
        }

        public static async Task MissingFileExecution(IWebMClient mClient)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);

            //var missing = await GetMissingFiles(mClient);
            var missing = await GetSpecialMissingFiles(mClient);
            var uploads = TestMode ? missing.Take(1).ToList() : missing;
            Console.WriteLine($"Missing file count: {uploads.Count}");
            FileLogger.Log("Program.GetMissingFiles.", $"Missing file count: {uploads.Count}");

            await uploadMgr.UploadMissingFiles(uploads);
            await uploadMgr.UploadLargeFileLocalDirectory();

            var gpm = new ConaEcommProductAssetDetailer(mClient, uploadMgr.DirectoryFileUploadResponses);
            await gpm.UpdateAllAssets();
            //await gpm.SaveAllAssets();

            //if (gpm._failedAssets.Any())
            //{
            //    FileLogger.Log("Program.GetMissingFiles", $"Failed Assets:");
            //    ICollection<FileUploadResponse> failedFiles = new List<FileUploadResponse>();
            //    foreach (var ff in gpm._failedAssets)
            //    {
            //        var uploadFailedFile = await uploadMgr.UploadLocalFile(ff.OriginPath);
            //        failedFiles.Add(uploadFailedFile);
            //    }

            //    var gpmRetry = new ConaEcommProductAssetDetailer(mClient, failedFiles);
            //    await gpmRetry.UpdateAllAssets();
            //    await gpmRetry.SaveAllAssets();

            //    var ffc = $"Failed files count: {gpmRetry._failedAssets.Count}";
            //    Console.WriteLine(ffc);
            //    FileLogger.Log("Program.GetMissingFiles", ffc);

            //    foreach (var failed in gpmRetry._failedAssets)
            //    {
            //        var fp = $"{failed.OriginPath}";
            //        Console.WriteLine(fp);
            //        FileLogger.Log("Program.GetMissingFiles", fp);
            //        FileLogger.AddToFailedUploadLog(failed.OriginPath);
            //    }
            //}

            Console.WriteLine($"Completed {gpm.ActuallySaved}");
            FileLogger.Log("Program.GetMissingFiles", $"Completed {gpm.ActuallySaved}");
        }

        public static async Task MissingFileExecutionUsingLogicApp(IWebMClient mClient)
        {
            List<Task> logicAppTasks = new List<Task>();
            List<(string path, Task taskExe)> fucntionTasks = new List<(string path, Task taskExe)>();
            List<string> manulChunking = new List<string>();

            List<FileUploadResponse> httpResponseMessages = new List<FileUploadResponse>();

            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);

            var missing = await GetMissingFiles(mClient);
            var missingLoop = TestMode ? missing.Take(TestModeTake).ToList() : missing;

            Console.WriteLine($"Missing file count: {missingLoop.Count}");
            FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", $"Missing file count: {missingLoop.Count}");

            foreach (var missed in missingLoop)
            {
                var fileInfo = new FileInfo(missed.LocalPath);
                if (fileInfo.Length > DropboxLogicApp.MaxFileSize)
                {
                    //manulChunking.Add(missed.LocalPath);
                    httpResponseMessages.Add(new FileUploadResponse(0, missed.LocalPath));
                }
                else if (fileInfo.Length > _contenthubMaxFileSize && fileInfo.Length < DropboxLogicApp.MaxFileSize)
                {
                    var log = $"Adding missed file {missed} to LargeFileUploadFunction tasks - {fileInfo.Length} bytes.";
                    
                    var largeFunc = new LargeFileUploadFunction();
                    var funcReq = new LargeFileFunctionRequest((string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken, GetFileContentAsBase64(missed.LocalPath), fileInfo.Length, fileInfo.Name, UploadManager.GetMediaType(fileInfo.Extension), (string)Configuration["ContentHubUploadConfiguration"]);

                    fucntionTasks.Add((missed.LocalPath, largeFunc.Send(funcReq)));

                    Console.WriteLine(log);
                    FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", log);
                }
                else
                {
                    var log = $"Adding missed file {missed} to DropboxLogicApp tasks - {fileInfo.Length} bytes.";
                    Console.WriteLine($"Missing file count: {missingLoop.Count}");
                    FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", $"Missing file count: {missingLoop.Count}");

                    var drop = new DropboxLogicApp();
                    var req = new LogicAppRequest((string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken, (string)Configuration["ContentHubUploadConfiguration"], PathTrimmerLogicApp(missed.LocalPath, 4), false);
                    req.Filename = fileInfo.Name;

                    logicAppTasks.Add(drop.Send(req));

                    Console.WriteLine(log);
                    FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", log);
                }
            }

            await logicAppTasks.WhenAll();

            try
            {
                var processLog = $"Processing logic app tasks... - {fucntionTasks.Count}";
                Console.WriteLine(processLog);
                FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", processLog);

                foreach (var task in logicAppTasks)
                {
                    try
                    {
                        var result = ((Task<HttpResponseMessage>)task).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            var respData = await result.Content.ReadAsStringAsync();
                            var content = System.Text.Json.JsonSerializer.Deserialize<List<LogicAppResponse>>(respData);
                            var firstResponse = content.FirstOrDefault();
                            httpResponseMessages.Add(new FileUploadResponse(firstResponse.ContentHubReponse.Asset_Id, firstResponse.BoxPath));
                        }
                        else
                        {
                            var respData = await result.RequestMessage.Content.ReadAsStringAsync();
                            FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", $"LogicApp request Failed:\n  {respData}");
                        }

                    }
                    catch (Exception ex)
                    {
                        var processExLog = $"Error Processing logic app task - Exception: {ex.Message}";
                        Console.WriteLine(processLog);
                        FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", processLog);
                        continue;
                    }   
                }
            }
            catch { }

            await fucntionTasks.Select(s => s.taskExe).WhenAll();

            try
            {
                var processLog = $"Processing large file function app tasks... - {fucntionTasks.Count}";
                Console.WriteLine(processLog);
                FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", processLog);

                foreach (var task in fucntionTasks)
                {
                    try
                    {
                        var result = ((Task<HttpResponseMessage>)task.taskExe).Result;
                        if (result.IsSuccessStatusCode)
                        {
                            var respData = await result.Content.ReadAsStringAsync();
                            var content = JsonConvert.DeserializeObject<LargeFileFunctionResponse>(respData);
                            httpResponseMessages.Add(new FileUploadResponse(content.Asset_id, task.path));
                        }
                        else
                        {
                            var respData = await result?.Content?.ReadAsStringAsync();
                            Console.WriteLine($"LargeFile function request Failed:\n  {respData}.\n Chunking manually.");
                            FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", $"LargeFile function request Failed:\n  {respData}");
                            httpResponseMessages.Add(new FileUploadResponse(0, task.path));
                        }
                    }
                    catch (Exception ex)
                    {
                        var processExLog = $"Error Processing large file function app task - {task.path}\n Exception: {ex.Message}";
                        Console.WriteLine(processLog);
                        FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", processLog);
                        continue;
                    }
                }
            }
            catch { }

            var uploads = httpResponseMessages;
            Console.WriteLine($"Logic App upload count: {uploads.Count}");
            FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", $"Logic App upload count: {uploads.Count}");

            List<Task> azureFailed = new List<Task>();
            if (uploads.Any(a => a.AssetId == 0))
            {
                var assetZero = uploads.Where(w => w.AssetId == 0).ToList();

                foreach (var fi in assetZero)
                {
                    uploads.Remove(fi);
                    azureFailed.Add(uploadMgr.UploadLargeLocalFile(fi.LocalPath));
                }

                await azureFailed.WhenAll();

                try
                {
                    var azProcessLog = $"Processing large files that failed in Azure - {azureFailed.Count}";
                    Console.WriteLine(azProcessLog);
                    FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", azProcessLog);

                    foreach (var task in azureFailed)
                    {
                        var result = ((Task<FileUploadResponse>)task).Result;
                        if (result.AssetId > 0)
                        {
                            uploads.Add(result);
                        }
                        else
                        {
                            var mChunkingLog = $"Manual chunking and upload failed - {result.LocalPath}";
                            Console.WriteLine(mChunkingLog);
                            FileLogger.Log("Program.MissingFileExecutionUsingLogicApp.", mChunkingLog);
                        }
                    }
                }
                catch { }

            }

            var gpm = new ConaEcommProductAssetDetailer(mClient, uploads);
            await gpm.UpdateAllAssets();
            await gpm.SaveAllAssets();

            if (gpm._failedAssets.Any())
            {
                FileLogger.Log("Program.MissingFileExecutionUsingLogicApp", $"Failed Assets:");
                foreach (var failed in gpm._failedAssets)
                {
                    var fp = $"{failed.OriginPath} \n Error: {failed.Errors.FirstOrDefault()}";
                    Console.WriteLine(fp);
                    FileLogger.Log("Program.MissingFileExecutionUsingLogicApp", fp);
                    FileLogger.AddToFailedUploadLog(failed.OriginPath);
                }
            }

            Console.WriteLine($"Completed {gpm._covetrusAsset.Count}");
            FileLogger.Log("Program.MissingFileExecutionUsingLogicApp", $"Completed {gpm._covetrusAsset.Count}");
        }

        private static string GetFileContentAsBase64(string localPath)
        {
            Byte[] bytes = System.IO.File.ReadAllBytes(localPath);
            return Convert.ToBase64String(bytes);
        }

        public static async Task ReloadAssetsWithZeroFileSizeExecution(IWebMClient mClient)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);
            //var directoryPath = ConaEcommProductAssetDetailer.UploadPath;
            var uploads = await GetZeroFiles(mClient);
            await uploadMgr.UploadLocalDirectoryVersions(uploads);
            await uploadMgr.UploadLargeFileLocalDirectoryVersions();

            Console.WriteLine($"Reloading of assets completed {uploads.Count}");
            FileLogger.Log("Program", $"Reloading of assets completed {uploads.Count}");
        }

        public static async Task ReloadModifiedAssetsExecution(IWebMClient mClient)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);
            //var directoryPath = ConaEcommProductAssetDetailer.UploadPath;
            var uploads = await GetModifiedFiles(mClient);
            await uploadMgr.UploadLocalDirectoryVersions(uploads);
            await uploadMgr.UploadLargeFileLocalDirectoryVersions();

            Console.WriteLine($"Reloading of assets completed {uploads.Count}");
            FileLogger.Log("Program", $"Reloading of assets completed {uploads.Count}");
        }

        public static async Task MigratedAssetsWithNoTypeExecution(IWebMClient mClient, bool checkOriginPath)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);
            //var directoryPath = ConaEcommProductAssetDetailer.UploadPath;
            //await uploadMgr.UploadLocalDirectory(directoryPath, SearchOption.AllDirectories);
            //await uploadMgr.UploadLargeFileLocalDirectory();

            var em = new EntityManager(mClient);
            //var mig = await em.GetMigratedAssetsWithNoType(checkOriginPath);
            var mig = await em.GetMigratedAssetsWithPathAndNoType(checkOriginPath);

            var gpm = new ConaEcommProductAssetDetailer(mClient, mig);// uploadMgr.DirectoryFileUploadResponses);
            await gpm.UpdateAllAssets();
            //await gpm.SaveAllAssets();

            //if (gpm._failedAssets.Any())
            //{
            //    FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", $"Failed Assets:");
            //    ICollection<FileUploadResponse> failedFiles = new List<FileUploadResponse>();
            //    foreach (var ff in gpm._failedAssets)
            //    {
            //        var uploadFailedFile = await uploadMgr.UploadLocalFile(ff.OriginPath);
            //        failedFiles.Add(uploadFailedFile);
            //    }

            //    var gpmRetry = new ConaEcommProductAssetDetailer(mClient, failedFiles);
            //    await gpmRetry.UpdateAllAssets();
            //    await gpmRetry.SaveAllAssets();

            //    var ffc = $"Failed files count: {gpmRetry._failedAssets.Count}";
            //    Console.WriteLine(ffc);
            //    FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", ffc);

            //    foreach (var failed in gpmRetry._failedAssets)
            //    {
            //        var fp = $"{failed.OriginPath}";
            //        Console.WriteLine(fp);
            //        FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", fp);
            //        FileLogger.AddToFailedUploadLog(failed.OriginPath);
            //    }
            //}

            Console.WriteLine($"Completed {gpm.ActuallySaved}");
            FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", $"Completed {gpm.ActuallySaved}");
        }

        public static async Task MigratedAssetsWithNoAssignedProduct(IWebMClient mClient)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);
            //var directoryPath = ConaEcommProductAssetDetailer.UploadPath;
            //await uploadMgr.UploadLocalDirectory(directoryPath, SearchOption.AllDirectories);
            //await uploadMgr.UploadLargeFileLocalDirectory();

            var em = new ProductManager(mClient);
            var mig = await em.GetMigratedAssetsWithAssignedProducts();

            var gpm = new DesignSmartPakProductAssetDetailer(mClient, mig);// uploadMgr.DirectoryFileUploadResponses);
            await gpm.SaveAllAssets();

            if (gpm._failedAssets.Any())
            {
                FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", $"Failed Assets:");
                ICollection<FileUploadResponse> failedFiles = new List<FileUploadResponse>();
                foreach (var ff in gpm._failedAssets)
                {
                    var uploadFailedFile = await uploadMgr.UploadLocalFile(ff.OriginPath);
                    failedFiles.Add(uploadFailedFile);
                }

                var gpmRetry = new DesignSmartPakProductAssetDetailer(mClient, failedFiles);
                await gpmRetry.UpdateAllAssets();
                //await gpmRetry.SaveAllAssets();

                var ffc = $"Failed files count: {gpmRetry._failedAssets.Count}";
                Console.WriteLine(ffc);
                FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", ffc);

                foreach (var failed in gpmRetry._failedAssets)
                {
                    var fp = $"{failed.OriginPath}";
                    Console.WriteLine(fp);
                    FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", fp);
                    FileLogger.AddToFailedUploadLog(failed.OriginPath);
                }
            }

            Console.WriteLine($"Completed {gpm._covetrusAsset.Count}");
            FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", $"Completed {gpm._covetrusAsset.Count}");
        }

        public static async Task MigratedAssetsWithNoAssignedCatalog(IWebMClient mClient)
        {
            var uploadMgr = new UploadManager(mClient, (string)Configuration["Sandboxes:0:Covetrus"], _contentHubToken);
            //var directoryPath = ConaEcommProductAssetDetailer.UploadPath;
            //await uploadMgr.UploadLocalDirectory(directoryPath, SearchOption.AllDirectories);
            //await uploadMgr.UploadLargeFileLocalDirectory();

            var em = new ProductManager(mClient);
            var mig = await em.GetMigratedAssetsWithAssignedCatalog();

            var gpm = new DesignSmartPakProductAssetDetailer(mClient, mig);// uploadMgr.DirectoryFileUploadResponses);
            await gpm.SaveAllAssets();

            if (gpm._failedAssets.Any())
            {
                FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", $"Failed Assets:");
                ICollection<FileUploadResponse> failedFiles = new List<FileUploadResponse>();
                foreach (var ff in gpm._failedAssets)
                {
                    var uploadFailedFile = await uploadMgr.UploadLocalFile(ff.OriginPath);
                    failedFiles.Add(uploadFailedFile);
                }

                var gpmRetry = new DesignSmartPakProductAssetDetailer(mClient, failedFiles);
                await gpmRetry.UpdateAllAssets();
                await gpmRetry.SaveAllAssets();

                var ffc = $"Failed files count: {gpmRetry._failedAssets.Count}";
                Console.WriteLine(ffc);
                FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", ffc);

                foreach (var failed in gpmRetry._failedAssets)
                {
                    var fp = $"{failed.OriginPath}";
                    Console.WriteLine(fp);
                    FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", fp);
                    FileLogger.AddToFailedUploadLog(failed.OriginPath);
                }
            }

            Console.WriteLine($"Completed {gpm._covetrusAsset.Count}");
            FileLogger.Log("Program.MigratedAssetsWithNoTypeExecution", $"Completed {gpm._covetrusAsset.Count}");
        }

        private static async Task<List<FileUploadResponse>> GetZeroFiles(IWebMClient mClient)
        {
            var results = new List<FileUploadResponse>();

            var query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.Property("OriginPath").Contains("Categories") 
                    && e.Property("OriginPath").Contains("CS")
                    && e.CreatedOn > DateTime.Today.AddDays(-1)
                    && e.Property("Filesize") == 0
                  select e).Skip(0).Take(3000)); ;
            var mq = await mClient.Querying.QueryAsync(query);
            var items = mq.Items.ToList();
            var zeroFiles = IsVirtualMachine ? items.Select(s => new { Id = s.Id.Value, Filename = $@"E:\Dropbox (Covetrus){(string)s.GetPropertyValue("OriginPath")}" }).ToList() : items.Select(s => new { Id = s.Id.Value, Filename = $@"C:\Users\ptjhi{(string)s.GetPropertyValue("OriginPath")}" }).ToList();

            foreach (var zFfile in zeroFiles)
            {
                results.Add(new FileUploadResponse(zFfile.Id, zFfile.Filename));
            }

            return results;
        }

        private static async Task<List<FileUploadResponse>> GetModifiedFiles(IWebMClient mClient)
        {
            var results = new List<FileUploadResponse>();
            Dictionary<string, string> filenames = new Dictionary<string, string>();

            var directoryPath = OriginFolder;
            var files = Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (var file in files.Distinct())
            {
                DateTime modifyTime = System.IO.File.GetLastWriteTime(file);
                if (modifyTime > DateTime.Now.AddDays(-9))
                {
                    var fileInfo = new FileInfo(file);
                    filenames.Add(fileInfo.FullName, fileInfo.Name);

                    var query = Query.CreateQuery(entities =>
                     (from e in entities
                      where e.Property("OriginPath").Contains("Product Images")
                        && e.Property("OriginPath").Contains("Hybris Ready")
                        && e.CreatedOn > DateTime.Today.AddDays(-9)
                        && e.Property("Title") == fileInfo.Name
                      select e).Skip(0).Take(1000));
                    var mq = await mClient.Querying.QueryAsync(query);
                    var items = mq.Items.ToList();
                    var zeroFiles = IsVirtualMachine ? items.Select(s => new { Id = s.Id.Value, Filename = $@"F:{(string)s.GetPropertyValue("OriginPath")}" }).ToList() : items.Select(s => new { Id = s.Id.Value, Filename = $@"C:\Users\ptjhi{(string)s.GetPropertyValue("OriginPath")}" }).ToList();

                    foreach (var zFfile in zeroFiles)
                    {
                        results.Add(new FileUploadResponse(zFfile.Id, zFfile.Filename));
                    }
                }
            }

            return results;
        }

        private static async Task<List<FileUploadResponse>> GetMissingFiles(IWebMClient mClient)
        {
            Dictionary<string, string> filenames = new Dictionary<string, string>();
            List<string> qFilenames = new List<string>();
            int curSkip = 0;
            int curTake = 5000;
            bool canQuery = true;

            while (canQuery)
            {
                var query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.Property("OriginPath").Contains("VCP")
                    && e.Parent("AssetTypeToAsset") == null
                    //&& e.Property("OriginPath").Contains("Manufacturer Originals")
                    //&& e.Property("OriginPath").Contains("V036")
                    //&& e.Property("Title").StartsWith("V050")
                    //&& e.CreatedOn > DateTime.Today.AddDays(-5)
                  select e).Skip(curSkip).Take(curTake));
                var mq = await mClient.Querying.QueryAsync(query);
                if (mq.TotalNumberOfResults > 0)
                {
                    var items = mq.Items.ToList();
                    qFilenames.AddRange(items.Select(s => (string)s.GetPropertyValue("Filename")).ToList());
                    curSkip = curSkip + curTake;
                    if (items.Count < curTake)
                    {
                        canQuery = false;
                    }
                }
                else
                {
                    canQuery = false;
                }
            }

            var directoryPath = OriginFolder;
            var files = Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (var file in files.Distinct())
            {
                var fileInfo = new FileInfo(file);
                filenames.Add(fileInfo.FullName, fileInfo.Name);
            }

            foreach (var chFile in qFilenames)
            {
                var keyToRemove = filenames.Where(w => w.Value == chFile).Select(s => s.Key).FirstOrDefault();
                if (!String.IsNullOrEmpty(keyToRemove))
                {
                    filenames.Remove(keyToRemove);
                }
            }

            var results = new List<FileUploadResponse>();

            for (int i = 0; i < filenames.Values.Count(); i++)
            {
                if (!results.Any(a => a.LocalPath.Equals(filenames.ElementAt(i).Key)))
                {
                    results.Add(new FileUploadResponse(0, filenames.ElementAt(i).Key));
                }
            }

            return results;
        }

        private static async Task<List<FileUploadResponse>> GetSpecialMissingFiles(IWebMClient mClient)
        {
            var directoryPath = OriginFolder;
            var files = Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories).ToList();

            Dictionary<string, string> filenames = new Dictionary<string, string>();
            List<string> qFilenames = new List<string>();
            int skip = 0;
            int take = 500;
            bool continueLoop = true;

            while (continueLoop)
            {
                var query = Query.CreateQuery(entities =>
                 (from e in entities
                  where e.Property("OriginPath").Contains("Dropbox (Covetrus)")
                        && e.Property("OriginPath").Contains("CONA Creative Campaigns")
                        && e.Property("OriginPath").Contains("2021")
                        && e.Parent("BusinessDomainToAsset").In(32585)
                    //&& e.Property("OriginPath").Contains("V036")
                    //&& e.Property("Title").StartsWith("300")
                    && e.CreatedOn > DateTime.Today.AddDays(-6)
                  select e).Skip(skip).Take(take));
                var mq = await mClient.Querying.QueryAsync(query);
                var items = mq.Items.ToList();
                qFilenames.AddRange(items.Select(s => "C:\\Users\\ptjhi" + (string)s.GetPropertyValue("OriginPath")).ToList());

                foreach (var item in qFilenames)
                {
                    files.Remove(item);
                }

                if (qFilenames.Count() >= mq.TotalNumberOfResults)
                {
                    continueLoop = false;
                }
                else
                {
                    skip = skip + take;
                    
                }
            }


            //var directoryPath = OriginFolder;
            //var files = Directory.GetFiles(directoryPath, "*.*", System.IO.SearchOption.AllDirectories);
            foreach (var file in files.Distinct())
            {
                var fileInfo = new FileInfo(file);
                filenames.Add(fileInfo.FullName, fileInfo.Name);
            }

            foreach (var chFile in qFilenames)
            {
                var keyToRemove = filenames.Where(w => w.Value == chFile).Select(s => s.Key).FirstOrDefault();
                if (!String.IsNullOrEmpty(keyToRemove))
                {
                    filenames.Remove(keyToRemove);
                }
            }

            var results = new List<FileUploadResponse>();

            for (int i = 0; i < filenames.Values.Count(); i++)
            {
                if (!results.Any(a => a.LocalPath.Equals(filenames.ElementAt(i).Key)))
                {
                    results.Add(new FileUploadResponse(0, filenames.ElementAt(i).Key));
                }
            }

            return results;
        }


        static async Task<bool> IsSandboxRunning(string sandboxUrl)
        {
            var httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(sandboxUrl);

            var resp = await httpClient.GetAsync("");

            return resp.IsSuccessStatusCode;
        }

        public static List<FileUploadResponse> FileUploadResponses => new List<FileUploadResponse>
        {
        };

        static void RunAsAService()
        {
            ServiceBase[] servicesToRun = new ServiceBase[]
           {
                new ContentHubService(_serviceName)
           };
            ServiceBase.Run(servicesToRun);
        }

        public static void StopWindowsService()
        {
            FileLogger.Log("Program", "Stopping service.");


            ServiceController serviceController = new ServiceController(_serviceName);
            serviceController.Stop();
        }

        public static void CreateLogFile()
        {
            FileLogger.SetLogFileName(_serviceName);
            string executableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string location = Path.Combine(executableLocation, $"{_serviceName}.txt");

            System.IO.File.WriteAllText(location, "Starting.");
        }
    }
}
