using System;
using System.IO;
using System.Threading.Tasks;

namespace MagicConchBot.Services {
	public class SoundCloudResolveClient : SoundCloudConnector {
        public const string Part = "resolve";

        public SoundCloudResolveClient(string clientId) : base(clientId, null) {
            httpClient.BaseAddress = new Uri(new Uri(BaseUrl), Part);
        }


        public Task<Track> GetTrack(string url) {


            queryParts.Add("url", url);
            return Get<Track>("");
		}
	}

    public class User
    {
        public int Id { get; set; }
        public string Kind { get; set; }
        public string Permalink { get; set; }
        public string Username { get; set; }
        public string LastModified { get; set; }
        public string Uri { get; set; }
        public string PermalinkUrl { get; set; }
        public string AvatarUrl { get; set; }
    }

    public class Track
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public string CreatedAt { get; set; }
        public int UserId { get; set; }
        public int Duration { get; set; }
        public bool Commentable { get; set; }
        public string State { get; set; }
        public int OriginalContentSize { get; set; }
        public string LastModified { get; set; }
        public string Sharing { get; set; }
        public string TagList { get; set; }
        public string Permalink { get; set; }
        public bool Streamable { get; set; }
        public string EmbeddableBy { get; set; }
        public string PurchaseUrl { get; set; }
        public string PurchaseTitle { get; set; }
        public string LabelId { get; set; }
        public string Genre { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string LabelName { get; set; }
        public string Release { get; set; }
        public string TrackType { get; set; }
        public string KeySignature { get; set; }
        public object Isrc { get; set; }
        public string VideoUrl { get; set; }
        public int? Bpm { get; set; }
        public object ReleaseYear { get; set; }
        public object ReleaseMonth { get; set; }
        public object ReleaseDay { get; set; }
        public string OriginalFormat { get; set; }
        public string License { get; set; }
        public string Uri { get; set; }
        public User User { get; set; }
        public string PermalinkUrl { get; set; }
        public string ArtworkUrl { get; set; }
        public string StreamUrl { get; set; }
        public string DownloadUrl { get; set; }
        public int PlaybackCount { get; set; }
        public int DownloadCount { get; set; }
        public int FavoritingsCount { get; set; }
        public int RepostsCount { get; set; }
        public int CommentCount { get; set; }
        public bool Downloadable { get; set; }
        public string WaveformUrl { get; set; }
        public string AttachmentsUri { get; set; }
    }
}