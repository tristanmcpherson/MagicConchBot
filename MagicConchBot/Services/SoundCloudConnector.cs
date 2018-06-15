using System.Net.Http;

namespace MagicConchBot.Services {
	internal class SoundCloudConnector {
		private HttpClient httpClient;
		private const string BaseUrl = "https://api.soundcloud.com";

		public SoundCloudConnector(string clientId, string clientSecret) {
			httpClient = new HttpClient();
		}

		internal IUnauthorizedSoundCloudClient UnauthorizedConnect() {
			return null;
		}

		public Track GetTrack(string trackId) {
			var url = BaseUrl.MergeUrl("tracks").MergeUrl(trackId);
			return null;
		}
	}
}