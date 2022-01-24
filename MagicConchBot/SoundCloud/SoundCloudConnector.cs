using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace MagicConchBot.Services
{
    public record OAuthResponse(
        string access_token,
        int expires_in,
        string refresh_token,
        string scope,
        string token_type
    );

    public record ResolveResponse(
        string status,
        string location
    );

    public class SoundCloudConnector
    {
        internal readonly HttpClient httpClient;
        internal const string BaseUrl = "https://api.soundcloud.com/";
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        private readonly Uri baseUri = new(BaseUrl);

        private DateTime _lastRequest = DateTime.MinValue;
        private int _expires = 0;
        private string _refreshToken = null;
        //private string _token = 
        private string _token = null;

        public SoundCloudConnector(string clientId, string clientSecret)
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

            
            //_credentialCache.Add()
            //httpClient.DefaultRequestHeaders.Authorization = new("OAuth", _token);
            //httpClient.DefaultRequestHeaders.Accept.Add(new("application/json"));
        }

        public async Task<ResolveResponse> Resolve(string url)
        {
            await EnsureAuthenticated();
            var resolveUri = new Uri(baseUri, "resolve");
            var uriBuilder = new UriBuilder(resolveUri);
            var queryBuilder = HttpUtility.ParseQueryString(string.Empty);
            queryBuilder.Add("url", url);
            uriBuilder.Query = queryBuilder.ToString();
            var uri = uriBuilder.Uri;

            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new("OAuth", _token);
            request.Headers.Accept.Add(new("application/json"));


            var response = await httpClient.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ResolveResponse>(content);
        }

        public async Task<T> Get<T>(string url)
        {
            await EnsureAuthenticated();

            var uri = new Uri(url);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new("OAuth", _token);
            request.Headers.Accept.Add(new("application/json"));

            var response = await httpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(content);
        }

        internal static IAuthorizedSoundCloudClient AuthorizedConnect(string clientId, string clientSecret)
        {
            return new UnauthorizedSoundCloudClient(clientId, clientSecret);
        }
    }
}