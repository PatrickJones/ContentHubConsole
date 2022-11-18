using System;
using System.IO;
using System.Threading.Tasks;
using ReneWiersma.Chunking;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Data;
using static System.Net.WebRequestMethods;
using System.Text.Json;
using Newtonsoft.Json;

namespace ContentHubConsole.Assets
{
    public class LargeFileUpload
    {
        const string MIME_TYPE = "application/json";
        const string TOKEN_HEADER = "X-Auth-Token";
        const string DEFAULT_UPLOAD_CONFIGURATION = "AssetUploadConfiguration";
        const string FINALIZE_UPLOAD_ROUTE = "api/v2.0/upload/finalize";
        const string UPLOAD_ROUTE = "api/v2.0/upload";
        const string AZURE_FUNCTION_URL = "http://localhost:7071"; //"https://contenthublargefileuploadxc.azurewebsites.net";
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public async Task<LargeUploadResponse> UploadAzureFunction(LargeUploadRequest largeUploadRequest)
        {
            HttpClient client = new HttpClient();

            try
            {
                Console.WriteLine("LargeFileUpload HTTP trigger function recieved a request.");

                LargeUploadRequest data = largeUploadRequest;

                client.Timeout = TimeSpan.FromHours(1);
                client.BaseAddress = new Uri(AZURE_FUNCTION_URL);
                client.DefaultRequestHeaders.Clear();
                //client.DefaultRequestHeaders.Add(TOKEN_HEADER, data.ContentHubToken);

                var ms = new MemoryStream();
                await System.Text.Json.JsonSerializer.SerializeAsync(ms, new
                {
                    data.Filename,
                    data.MediaType,
                    data.FileSize,
                    FileContent = Convert.ToBase64String(data.FileContent),
                    data.ContentHubHostName,
                    data.ContentHubToken,
                    data.UploadConfiguration
                });
                ms.Seek(0, SeekOrigin.Begin);

                var request = new HttpRequestMessage(HttpMethod.Post, "api/largefileupload");
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                using (var requestContent = new StreamContent(ms))
                {
                    request.Content = requestContent;
                    requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStreamAsync();
                        Console.WriteLine("Function processing complete.");
                        return await System.Text.Json.JsonSerializer.DeserializeAsync<LargeUploadResponse>(content, _options);
                    }
                }





                //StringContent functionContent = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(new {
                //    data.Filename,
                //    data.MediaType,
                //    data.FileSize,
                //    FileContent = Convert.ToBase64String(data.FileContent),
                //    data.ContentHubHostName,
                //    data.ContentHubToken,
                //    data.UploadConfiguration
                //}), Encoding.UTF8, MIME_TYPE);

                //var resp = await client.PostAsync("api/largefileupload", functionContent);
                //if (!resp.IsSuccessStatusCode)
                //{
                //    var msg = await resp.Content.ReadAsStringAsync();
                //    Console.WriteLine($"Bad request: {msg}");
                //    return new LargeUploadResponse { Message = msg, Success = false };
                //}

                //var respContent = await resp.Content.ReadAsStringAsync();

                //Console.WriteLine("Function processing complete.");
                //return Newtonsoft.Json.JsonConvert.DeserializeObject<LargeUploadResponse>(respContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return new LargeUploadResponse { Asset_id = 0, Success = false };
            }
        }


        public async Task<LargeUploadResponse> Upload(LargeUploadRequest largeUploadRequest, long assetId = 0)
        {
            HttpClient client = new HttpClient();

            try
            {
                Console.WriteLine("LargeFileUpload recieved a request.");
                FileLogger.Log("Upload", "LargeFileUpload recieved a request.");


                LargeUploadRequest data = largeUploadRequest;
                client.Timeout = TimeSpan.FromHours(1);
                client.BaseAddress = new Uri(data.ContentHubHostName);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MIME_TYPE));
                client.DefaultRequestHeaders.Add(TOKEN_HEADER, data.ContentHubToken);

                Dictionary<int, byte[]> fileChunks = GetFileChunks(data.FileContent);

                KeyValuePair<string, HttpContent> upload = new KeyValuePair<string, HttpContent>();
                if (assetId > 0)
                {
                    upload = await GetVersionUploadUrl(data.ContentHubHostName, data.Filename, data.FileSize, data.ContentHubToken, data.UploadConfiguration, assetId);
                }
                else
                {
                    upload = await GetUploadUrl(data.ContentHubHostName, data.Filename, data.FileSize, data.ContentHubToken, data.UploadConfiguration);
                }

                foreach (var fileChunk in fileChunks.OrderBy(o => o.Key))
                {
                    var uploadUrl = $"{upload.Key}&chunks={fileChunks.Count}&chunk={fileChunk.Key}";

                    MultipartFormDataContent multipartContent = ConstructMultipartData(data, fileChunk);

                    var resp = await client.PostAsync(uploadUrl, multipartContent);
                    if (!resp.IsSuccessStatusCode)
                    {
                        var msg = await resp.Content.ReadAsStringAsync();
                        Console.WriteLine($"Bad request: {msg}");
                        FileLogger.Log("Upload", $"Bad request: {msg}");
                    }
                }

                await client.PostAsync($"{upload.Key}&chunks={fileChunks.Count}", null);

                var finalResp = await FinalizeUpload(data.ContentHubHostName, upload.Value, data.ContentHubToken);

                if (finalResp.Status == (int)HttpStatusCode.OK || finalResp.Status == (int)HttpStatusCode.Created || finalResp.Status == (int)HttpStatusCode.NoContent)
                {
                    Console.WriteLine("Function processing complete.");
                    FileLogger.Log("Upload", "Function processing complete.");
                    return finalResp.Response;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                FileLogger.Log("Upload", $"Exception: {ex.Message}");
                return new LargeUploadResponse { Asset_id = 0, Success = false };
            }

            Console.WriteLine("Function processing complete. No content.");
            FileLogger.Log("Upload", "Function processing complete. No content.");
            return new LargeUploadResponse { Asset_id = 0, Success = false };
        }

        /// <summary>
        /// Constructs multipart form-data content from file chunk
        /// </summary>
        /// <param name="data">Incoming request object</param>
        /// <param name="fileChunk">file chunk data</param>
        /// <returns>MultipartFormDataContent</returns>
        static MultipartFormDataContent ConstructMultipartData(LargeUploadRequest data, KeyValuePair<int, byte[]> fileChunk)
        {
            MultipartFormDataContent multipartContent = new MultipartFormDataContent();
            var byteArrayContent = new ByteArrayContent(fileChunk.Value);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue(data.MediaType);
            byteArrayContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                FileName = data.Filename,
                Name = "file",
            };
            multipartContent.Add(byteArrayContent);
            return multipartContent;
        }

        /// <summary>
        /// Divides file into 1000000 byte chunks
        /// https://docs.stylelabs.com/contenthub/4.1.x/content/integrations/rest-api/upload/upload-large-files.html
        /// </summary>
        /// <param name="fileData">file to chunk</param>
        /// <returns>Dictionary<int, byte[]></returns>
        static Dictionary<int, byte[]> GetFileChunks(byte[] fileData)
        {
            Console.WriteLine("Chunking file data...");
            FileLogger.Log("GetFileChunks", "Chunking file data...");
            var base64EncodedBytes = fileData;

            int chunkCounter = 0;
            Dictionary<int, byte[]> fileChunks = new Dictionary<int, byte[]>();
            foreach (var chunk in base64EncodedBytes.ToChunks(1000000)) //(1000000)) approx. 1 Megabyte chunks per https://docs.stylelabs.com/contenthub/4.1.x/content/integrations/rest-api/upload/upload-large-files.html
            {
                fileChunks.Add(chunkCounter, chunk.ToArray()); ;
                chunkCounter++;
            }

            Console.WriteLine($"File chunking completed: {chunkCounter++} total");
            FileLogger.Log("GetFileChunks", $"File chunking completed: {chunkCounter++} total");
            return fileChunks;
        }

        /// <summary>
        /// Gets upload URL from Content Hub
        /// https://docs.stylelabs.com/contenthub/4.1.x/content/integrations/rest-api/upload/upload-api-v2.html#request-an-upload
        /// </summary>
        /// <param name="hostUrl">Content Hub instance URL</param>
        /// <param name="fileName">Filename with extension</param>
        /// <param name="fileSize">File size</param>
        /// <param name="token">Content Hub access token</param>
        /// <param name="uploadConfiguration">Content Hub upload configuration</param>
        /// <param name="log">Logger</param>
        /// <returns>KeyValuePair<string, HttpContent></returns>
        static async Task<KeyValuePair<string, HttpContent>> GetVersionUploadUrl(string hostUrl, string fileName, long fileSize, string token, string uploadConfiguration, long assetId)
        {
            Console.WriteLine($"Getting upload url from Content Hub");
            FileLogger.Log("GetUploadUrl", $"Getting upload url from Content Hub");

            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromHours(1);
                client.BaseAddress = new Uri(hostUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MIME_TYPE));
                client.DefaultRequestHeaders.Add(TOKEN_HEADER, token);

                var payload = new
                {
                    action = new
                    {
                        name = "NewMainFile",
                        parameters = new { AssetId = assetId }
                    },
                    file_name = fileName,
                    file_size = fileSize.ToString(),
                    upload_configuration = new
                    {
                        name = String.IsNullOrEmpty(uploadConfiguration) ? DEFAULT_UPLOAD_CONFIGURATION : uploadConfiguration,
                        parameters = new { }
                    }
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                var resp = await client.PostAsync(UPLOAD_ROUTE, new StringContent(jsonPayload, Encoding.UTF8, MIME_TYPE));

                Console.WriteLine($"Getting upload url response: {resp.StatusCode}");
                FileLogger.Log("GetUploadUrl", $"Getting upload url response: {resp.StatusCode}");

                return resp.IsSuccessStatusCode
                    ? new KeyValuePair<string, HttpContent>(resp.Headers.GetValues("Location").FirstOrDefault(), resp.Content)
                    : new KeyValuePair<string, HttpContent>(String.Empty, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting upload url:\n {ex.Message}");
                FileLogger.Log("GetUploadUrl", $"Error getting upload url: {ex.Message}");
                throw;
            }
        }


        /// <summary>
        /// Gets upload URL from Content Hub
        /// https://docs.stylelabs.com/contenthub/4.1.x/content/integrations/rest-api/upload/upload-api-v2.html#request-an-upload
        /// </summary>
        /// <param name="hostUrl">Content Hub instance URL</param>
        /// <param name="fileName">Filename with extension</param>
        /// <param name="fileSize">File size</param>
        /// <param name="token">Content Hub access token</param>
        /// <param name="uploadConfiguration">Content Hub upload configuration</param>
        /// <param name="log">Logger</param>
        /// <returns>KeyValuePair<string, HttpContent></returns>
        static async Task<KeyValuePair<string, HttpContent>> GetUploadUrl(string hostUrl, string fileName, long fileSize, string token, string uploadConfiguration)
        {
            Console.WriteLine($"Getting upload url from Content Hub");
            FileLogger.Log("GetUploadUrl", $"Getting upload url from Content Hub");

            try
            {
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromHours(1);
                client.BaseAddress = new Uri(hostUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MIME_TYPE));
                client.DefaultRequestHeaders.Add(TOKEN_HEADER, token);

                var payload = new
                {
                    action = new
                    {
                        name = "NewAsset",
                        parameters = new { }
                    },
                    file_name = fileName,
                    file_size = fileSize.ToString(),
                    upload_configuration = new
                    {
                        name = String.IsNullOrEmpty(uploadConfiguration) ? DEFAULT_UPLOAD_CONFIGURATION : uploadConfiguration,
                        parameters = new { }
                    }
                };

                var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                var resp = await client.PostAsync(UPLOAD_ROUTE, new StringContent(jsonPayload, Encoding.UTF8, MIME_TYPE));

                Console.WriteLine($"Getting upload url response: {resp.StatusCode}");
                FileLogger.Log("GetUploadUrl", $"Getting upload url response: {resp.StatusCode}");

                return resp.IsSuccessStatusCode
                    ? new KeyValuePair<string, HttpContent>(resp.Headers.GetValues("Location").FirstOrDefault(), resp.Content)
                    : new KeyValuePair<string, HttpContent>(String.Empty, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting upload url:\n {ex.Message}");
                FileLogger.Log("GetUploadUrl", $"Error getting upload url: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Finalizes file upload.
        /// https://docs.stylelabs.com/contenthub/4.1.x/content/integrations/rest-api/upload/upload-api-v2.html#finalize-the-upload
        /// </summary>
        /// <param name="hostUrl">Content Hub instance URL</param>
        /// <param name="httpContent">Request payload</param>
        /// <param name="token">Content Hub access token</param>
        /// <param name="log">Logger</param>
        /// <returns>(int Status, LargeUploadResponse Response)</returns>
        static async Task<(int Status, LargeUploadResponse Response)> FinalizeUpload(string hostUrl, HttpContent httpContent, string token)
        {
            try
            {
                LargeUploadResponse largeUploadResponse = new LargeUploadResponse();
                HttpClient client = new HttpClient();
                client.Timeout = TimeSpan.FromHours(1);
                client.BaseAddress = new Uri(hostUrl);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MIME_TYPE));
                client.DefaultRequestHeaders.Add(TOKEN_HEADER, token);

                Console.WriteLine($"Finalizing upload.");
                FileLogger.Log("FinalizeUpload", $"Finalizing upload.");

                var resp = await client.PostAsync(FINALIZE_UPLOAD_ROUTE, httpContent);

                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    largeUploadResponse = JsonConvert.DeserializeObject<LargeUploadResponse>(json);

                    Console.WriteLine($"Finalized upload.");
                    FileLogger.Log("FinalizeUpload", $"Finalized upload.");
                    return ((int)resp.StatusCode, largeUploadResponse);
                }


                Console.WriteLine($"Finalize upload response: null");
                FileLogger.Log("FinalizeUpload", $"Finalize upload response: null");
                return ((int)resp.StatusCode, largeUploadResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error finalizing upload:\n {ex.Message}");
                FileLogger.Log("FinalizeUpload", $"Error finalizing upload:\n {ex.Message}");
                throw;
            }
        }
    }
}
