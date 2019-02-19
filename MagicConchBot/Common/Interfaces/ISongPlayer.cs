using System.Threading.Tasks;
using Discord.Audio;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface ISongPlayer
    {
        float Volume { get; set; }
        PlayerState PlayerState { get; }
        Task PlaySong(IAudioClient client, Song song);
        void Stop();
        void Pause();
    }

    public interface IFileProvider
    {
        Task<string> GetStreamingFile(Song song);
    }

    public interface ISongResolver
    {
        Task<string> GetSongStreamUrl(string uri);
    }
}
