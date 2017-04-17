using System.Collections.Concurrent;
using System.IO;
using MagicConchBot.Common.Interfaces;
using NLog;

namespace MagicConchBot.Services.Music
{
    public class MusicServiceProvider
    {
        private readonly ConcurrentDictionary<ulong, IMusicService> _musicServices =
            new ConcurrentDictionary<ulong, IMusicService>();

        private readonly ConcurrentDictionary<ulong, Mp3ConverterService> _mp3Services =
            new ConcurrentDictionary<ulong, Mp3ConverterService>();

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public MusicServiceProvider()
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");

            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch
            {
                Log.Debug("Failed to delete temp folder.");
            }
        }

        public void AddServices(ulong guildId, IMusicService service, Mp3ConverterService mp3Service)
        {
            if (!_musicServices.ContainsKey(guildId))
                _musicServices.TryAdd(guildId, service);

            if (!_mp3Services.ContainsKey(guildId))
                _mp3Services.TryAdd(guildId, mp3Service);
        }

        public IMusicService GetService(ulong guildId)
        {
            if (!_musicServices.TryGetValue(guildId, out IMusicService service))
            {
                Log.Error("Server music service was not created. Cancelling.");
            }

            return service;
        }

        public Mp3ConverterService GetMp3Service(ulong guildId)
        {
            if (!_mp3Services.TryGetValue(guildId, out Mp3ConverterService service))
            {
                Log.Error("Server mp3 service was not created, creating.");
                service = new Mp3ConverterService();
                _mp3Services.TryAdd(guildId, service);
            }

            return service;
        }

        public void StopAll()
        {
            foreach (var musicService in _musicServices)
                if (musicService.Value.Stop())
                    Log.Info($"Successfully stopped music for GuildId: {musicService.Key}");
        }
    }
}