using System;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e);
    public record SongCompletedArgs(IAudioClient Client, IMessageChannel MessageChannel, Song Song);

    public interface ISongPlayer
    {
        event AsyncEventHandler<SongCompletedArgs> OnSongCompleted;
        float GetVolume();
        void SetVolume(float value);
        void PlaySong(IAudioClient client, IMessageChannel messageChannel, Song song);
        bool IsPlaying();
        Task Stop();
        Task Pause();
    }

    public interface IFileProvider
    {
        Task<string> GetStreamingFile(Song song);
    }

    public enum MusicType
    {
	    YouTube = 0,
	    SoundCloud = 1,
        Spotify = 2,
	    Other
    }
}
