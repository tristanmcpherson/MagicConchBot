namespace MagicConchBot.Common.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Discord;

    using MagicConchBot.Common.Enums;
    using MagicConchBot.Common.Types;

    public interface IMusicService
    {
        int Volume { get; }

        Task PlayAsync(IUserMessage msg);

        bool Stop();

        bool Pause();

        bool Skip();

        void QueueSong(Song song);

        Song DequeueSong(int songNumber);

        void ClearQueue();

        int ChangeVolume(int volume);

        void ChangePlayMode(PlayMode mode);

        // Task BufferSong();
        Song GetCurrentSong();

        List<Song> QueuedSongs();

        Task<string> GenerateMp3Async();
    }
}