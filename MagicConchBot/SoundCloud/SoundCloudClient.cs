using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace MagicConchBot.Services
{

    public class SoundCloudClient
    {
        internal readonly HttpClient httpClient;
        internal const string BaseUrl = "https://api.soundcloud.com/";
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        private readonly Uri baseUri = new(BaseUrl);

        private DateTime _lastRequest = DateTime.MinValue;
        private int _expires = 0;
        private string _refreshToken = null;
        private string _token = null;

        public SoundCloudClient(string clientId, string clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            httpClient = new HttpClient(new HttpClientHandler
            {
                PreAuthenticate = true,
                AllowAutoRedirect = false
            });
        }

        private async Task EnsureAuthenticated()
        {
            if (DateTime.Now < _lastRequest.AddSeconds(_expires) && _refreshToken != null)
            {
                return;
            }

            var formData = new MultipartFormDataContent
            {
                { new StringContent(ClientId), "client_id" },
                { new StringContent(ClientSecret), "client_secret" },
            };

            if (_refreshToken == null)
            {
                formData.Add(new StringContent("client_credentials"), "grant_type");
            } else
            {
                formData.Add(new StringContent(_refreshToken), "refresh_token");
                formData.Add(new StringContent("client_credentials"), "refresh_token");
            }

            var response = await httpClient.PostAsync(new Uri(baseUri, "oauth2/token"), formData);

            var content = await response.Content.ReadFromJsonAsync<OAuthResponse>();

            _expires = content.expires_in;
            _lastRequest = DateTime.Now;
            _refreshToken = content.refresh_token;
            _token = content.access_token;
        }

        public async Task<T> Get<T>(string url)
        {
            var originalUri = new Uri(baseUri, "resolve");
            var uriBuilder = new UriBuilder(originalUri);
            var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
            queryBuilder.Add("url", url);
            uriBuilder.Query = queryBuilder.ToString();
            var uri = uriBuilder.Uri;

            var resolveResponse = await GetInner<ResolveResponse>(uri);
            return await GetInner<T>(resolveResponse.location);
        }

        public async Task<List<Track>> Search(string query)
        {
            var originalUri = new Uri(baseUri, "tracks");
            var uriBuilder = new UriBuilder(originalUri);
            var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
            queryBuilder.Add("q", query);
            uriBuilder.Query = queryBuilder.ToString();
            var uri = uriBuilder.Uri;

            var resolveResponse = await GetInner<ResolveResponse>(uri);
            return await GetInner<List<Track>>(resolveResponse.location);
        }

        private Task<T> GetInner<T>(string url)
        {
            return GetInner<T>(new Uri(url));
        }

        private async Task<T> GetInner<T>(Uri uri)
        {
            await EnsureAuthenticated();

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new("OAuth", _token);
            request.Headers.Accept.Add(new("application/json"));

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }
    }
}