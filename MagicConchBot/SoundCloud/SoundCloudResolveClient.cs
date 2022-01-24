using System;
using System.IO;
using System.Threading.Tasks;

namespace MagicConchBot.Services
{
    public class SoundCloudResolveClient : SoundCloudConnector
    {
        public const string Part = "resolve";

        public SoundCloudResolveClient(string clientId, string clientSecret) : base(clientId, clientSecret)
        {
            //httpClient.BaseAddress = new Uri(new Uri(BaseUrl), Part);
        }


        public async Task<Track> GetTrack(string url)
        {
            var resolve = await Resolve(url);

            return await Get<Track>(resolve.location);
        }
    }

    public class Track
    {
        public string kind { get; set; }
        public int id { get; set; }
        public string created_at { get; set; }
        public int duration { get; set; }
        public bool commentable { get; set; }
        public int comment_count { get; set; }
        public string sharing { get; set; }
        public string tag_list { get; set; }
        public bool streamable { get; set; }
        public string embeddable_by { get; set; }
        public object purchase_url { get; set; }
        public object purchase_title { get; set; }
        public string genre { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public object label_name { get; set; }
        public object release { get; set; }
        public object key_signature { get; set; }
        public object isrc { get; set; }
        public object bpm { get; set; }
        public object release_year { get; set; }
        public object release_month { get; set; }
        public object release_day { get; set; }
        public string license { get; set; }
        public string uri { get; set; }
        public User user { get; set; }
        public string permalink_url { get; set; }
        public string artwork_url { get; set; }
        public string stream_url { get; set; }
        public object download_url { get; set; }
        public string waveform_url { get; set; }
        public object available_country_codes { get; set; }
        public object secret_uri { get; set; }
        public object user_favorite { get; set; }
        public object user_playback_count { get; set; }
        public int playback_count { get; set; }
        public int download_count { get; set; }
        public int favoritings_count { get; set; }
        public int reposts_count { get; set; }
        public bool downloadable { get; set; }
        public string access { get; set; }
        public object policy { get; set; }
        public object monetization_model { get; set; }
    }

    public class User
    {
        public string avatar_url { get; set; }
        public int id { get; set; }
        public string kind { get; set; }
        public string permalink_url { get; set; }
        public string uri { get; set; }
        public string username { get; set; }
        public string permalink { get; set; }
        public string created_at { get; set; }
        public string last_modified { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string full_name { get; set; }
        public string city { get; set; }
        public string description { get; set; }
        public object country { get; set; }
        public int track_count { get; set; }
        public int public_favorites_count { get; set; }
        public int reposts_count { get; set; }
        public int followers_count { get; set; }
        public int followings_count { get; set; }
        public string plan { get; set; }
        public object myspace_name { get; set; }
        public object discogs_name { get; set; }
        public object website_title { get; set; }
        public object website { get; set; }
        public int comments_count { get; set; }
        public bool online { get; set; }
        public int likes_count { get; set; }
        public int playlist_count { get; set; }
        public Subscription[] subscriptions { get; set; }
    }

    public class Subscription
    {
        public Product product { get; set; }
    }

    public class Product
    {
        public string id { get; set; }
        public string name { get; set; }
    }
}