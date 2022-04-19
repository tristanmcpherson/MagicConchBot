namespace MagicConchBot.Services
{
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
}