namespace MagicConchBot.Services {
	public interface IAuthorizedSoundCloudClient {
		SoundCloudResolveClient Resolve { get; set; }
	}

    public class UnauthorizedSoundCloudClient : IAuthorizedSoundCloudClient
    {
        public SoundCloudResolveClient Resolve { get; set; }

        public UnauthorizedSoundCloudClient(string clientId, string clientSecret) {
            Resolve = new SoundCloudResolveClient(clientId, clientSecret);
        }
    }
}