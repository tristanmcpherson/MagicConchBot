using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace MagicConchBot.Services {
    public class SoundCloudConnector {
        internal readonly HttpClient httpClient;
        internal const string BaseUrl = "https://api.soundcloud.com/";
        public static string ClientId { get; set; }
        public static string ClientSecret { get; set; }

        internal Dictionary<string, string> queryParts;

        public SoundCloudConnector(string clientId, string clientSecret) {
            ClientId = clientId;
            ClientSecret = clientSecret;

            queryParts = new Dictionary<string, string>();
            httpClient = new HttpClient() {
                BaseAddress = new Uri(BaseUrl)
            };
        }

        private void AddAuthentication()
        {
            if (ClientId != null)
            {
                queryParts.Add("client_id", ClientId);
            }

            if (ClientSecret != null)
            {
                queryParts.Add("client_secret", ClientSecret);
            }
        }

        public async Task<T> Get<T>(string relativeUrl) {
            AddAuthentication();

            var partialUrl = QueryHelpers.AddQueryString(relativeUrl, queryParts);

            queryParts.Clear();

            var content = await httpClient.GetStringAsync(partialUrl);
            return JsonConvert.DeserializeObject<T>(content);
        }

		internal IUnauthorizedSoundCloudClient UnauthorizedConnect() {
			return new UnauthorizedSoundCloudClient(ClientId);
		}
	}
}