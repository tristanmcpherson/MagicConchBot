namespace MagicConchBotApp.Services {
	public interface IUnauthorizedSoundCloudClient {
		SoundCloudResolveClient Resolve { get; set; }
	}
}