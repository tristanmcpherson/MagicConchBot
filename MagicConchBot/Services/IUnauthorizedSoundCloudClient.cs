namespace MagicConchBot.Services {
	public interface IUnauthorizedSoundCloudClient {
		SoundCloudResolveClient Resolve { get; set; }
	}
}