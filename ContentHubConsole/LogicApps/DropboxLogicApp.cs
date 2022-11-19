using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Tavis.UriTemplates;

namespace ContentHubConsole.LogicApps
{
    public class DropboxLogicApp
    {
        public static long MaxFileSize = 52428800;
        public async Task<HttpResponseMessage> Send(LogicAppRequest request)
        {
            HttpClient _client = new HttpClient();
            _client.Timeout = TimeSpan.FromHours(1);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new StringContent(JsonConvert.SerializeObject(request));
            return await _client.PostAsync(Program.DropboxUrl, payload);
        }
    }
}
