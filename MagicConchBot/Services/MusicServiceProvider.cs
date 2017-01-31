using Discord.Commands;
using log4net;
using MagicConchBot.Common.Interfaces;
using System.Collections.Concurrent;

namespace MagicConchBot.Services
{
    public class MusicServiceProvider
    {
        public ConcurrentDictionary<ulong, IMusicService> _musicServices;
        private static readonly ILog Log = LogManager.GetLogger(typeof(MusicServiceProvider));

        public MusicServiceProvider()
        {
            _musicServices = new ConcurrentDictionary<ulong, IMusicService>();
        }

        public void AddService(ulong guildId, IMusicService service)
        {
            if (!_musicServices.ContainsKey(guildId))
                _musicServices.TryAdd(guildId, service);
        }

        public IMusicService GetService(ulong guildId)
        {
            if (!_musicServices.TryGetValue(guildId, out var service))
            {
                Log.Error("Server music service was not created, recreating.");
                service = new FfmpegMusicService();
                _musicServices.TryAdd(guildId, service);
            }
            return service;
        }

        public void StopAll()
        {
            foreach (var musicService in _musicServices)
            {
                if (!musicService.Value.Stop())
                {
                    Log.Error($"Failed to stop music service for GuildId: {musicService.Key}");
                }
            }
        }
    }
}