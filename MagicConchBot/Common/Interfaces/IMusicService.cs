using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Common.Interfaces
{
    public interface IMusicService
    {
        int Volume { get; set; }

        Song LastSong { get; }

        Song CurrentSong { get; }

        PlayMode PlayMode { get; set; }

        Task PlayAsync(IUserMessage msg);

        bool Stop();

        bool Pause();

        bool Skip();

        void QueueSong(Song song);

        Song DequeueSong(int songNumber);

        void ClearQueue();

        List<Song> QueuedSongs();
    }
}