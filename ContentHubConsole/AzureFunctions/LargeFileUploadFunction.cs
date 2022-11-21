using ContentHubConsole.LogicApps;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole.AzureFunctions
{
    public class LargeFileUploadFunction
    {
        public async Task<HttpResponseMessage> Send(LargeFileFunctionRequest request)
        {
            try
            {
                var log = $"Sending request to large file function app - {request.Filename}";
                Console.WriteLine(log);
                FileLogger.Log("DropboxLogicApp.Send.", log);

                HttpClient _client = new HttpClient();
                _client.Timeout = TimeSpan.FromHours(1);
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var payload = new StringContent(JsonConvert.SerializeObject(request));
                return await _client.PostAsync(Program.LargeFileFunctionUrl, payload);

            }
            catch (Exception ex)
            {
                var erLog = $"Error sending request to large file function app - {request.Filename}\nError:{ex.Message}";
                Console.WriteLine(erLog);
                FileLogger.Log("DropboxLogicApp.Send.", erLog);
                var httpResp = new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                httpResp.Content = new StringContent(ex.Message);
                return httpResp;
            }      
        }
    }
}
