namespace MagicConchBot.Services {
	public interface IUnauthorizedSoundCloudClient {
		SoundCloudResolveClient Resolve { get; set; }
	}

    public class UnauthorizedSoundCloudClient : IUnauthorizedSoundCloudClient
    {
        public SoundCloudResolveClient Resolve { get; set; }

        public UnauthorizedSoundCloudClient(string clientId) {
            Resolve = new SoundCloudResolveClient(clientId);
        }
    }
}