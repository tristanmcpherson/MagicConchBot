using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Discord;
using MagicConchBot.Common.Enums;
using MagicConchBot.Common.Types;
using MagicConchBot.Services.Music;

namespace MagicConchBot.Common.Interfaces
{
    public interface IMusicService
    {
        float GetVolume();

        void SetVolume(float value);

        List<Song> GetSongs();

        Maybe<Song> CurrentSong { get; }

        Maybe<Song> LastSong { get; }
        
        PlayMode PlayMode { get; set; }

        bool IsPlaying { get; }

        // Refactor GuildSettings to PlaySettings data record
        Task Play(IInteractionContext msg, GuildSettings settings);

        Task Stop();

        Task Pause();

        bool Skip();

        void QueueSong(Song song);

        Task<Maybe<Song>> RemoveSong(int songNumber);

        void ClearQueue();
    }
}