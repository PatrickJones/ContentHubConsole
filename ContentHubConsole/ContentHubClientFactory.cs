using Stylelabs.M.Sdk.Clients;
using Stylelabs.M.Sdk.WebClient;
using Stylelabs.M.Sdk.WebClient.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContentHubConsole
{
    internal class ContentHubClientFactory
    {
        readonly string _clientId;
        readonly string _clientSecret;
        readonly string _userName;
        readonly string _password;
        readonly string _connectionUrl = String.Empty;

        public ContentHubClientFactory(string clientId, string clientSecret, string userName, string password, string connectionUrl)
        {
            _clientId = clientId;
            _clientSecret = clientSecret;
            _userName = userName;
            _password = password;
            _connectionUrl = connectionUrl;
        }

        internal IWebMClient Client()
        {
            Uri endpoint = new Uri(_connectionUrl);
            return MClientFactory.CreateMClient(endpoint, GetOAthGrant());
        }

        internal IScriptsClient ScriptsClient()
        {
            Uri endpoint = new Uri(_connectionUrl);
            return MClientFactory.CreateMClient(endpoint, GetOAthGrant()).Scripts;
        }

        OAuthPasswordGrant GetOAthGrant()
        {
            return new OAuthPasswordGrant
            {
                ClientId = _clientId,
                ClientSecret = _clientSecret,
                UserName = _userName,
                Password = _password,
            };
        }
    }
}
