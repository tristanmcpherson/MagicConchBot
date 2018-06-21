using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBotApp.Common.Enums;
using MagicConchBotApp.Common.Types;

namespace MagicConchBotApp.Common.Interfaces
{
    public interface IMusicService
    {
        float Volume { get; set; }

        List<Song> SongList { get; }

        Song LastSong { get; }
        
        Song CurrentSong { get; }

        PlayMode PlayMode { get; set; }

        PlayerState PlayerState { get; }

        Task PlayAsync(ICommandContext msg);

        bool Stop();

        bool Pause();

        bool Skip();

        void QueueSong(Song song);

        Song RemoveSong(int songNumber);

        void ClearQueue();
    }
}