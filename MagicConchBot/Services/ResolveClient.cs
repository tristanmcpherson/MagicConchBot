using System;
using System.Threading.Tasks;

namespace MagicConchBotApp.Services {
	public class SoundCloudResolveClient {
		public Task<Track> GetTrack(string url) {
			return Task.FromResult<Track>(null);
		}
	}

	public class Track {
		public string Title { get; set; }
		public TimeSpan Duration { get; set; }
		public Artwork Artwork { get; internal set; }
	}

	public class Artwork {
		public string Url { get; set; }
	}
}