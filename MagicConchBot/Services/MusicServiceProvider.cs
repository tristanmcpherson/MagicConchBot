using System.Collections.Concurrent;
using System.IO;
using MagicConchBot.Common.Interfaces;
using NLog;

namespace MagicConchBot.Services
{
    public static class MusicServiceProvider
    {
        private static readonly ConcurrentDictionary<ulong, IMusicService> MusicServices = new ConcurrentDictionary<ulong, IMusicService>();
        private static readonly ConcurrentDictionary<ulong, Mp3ConverterService> Mp3Services = new ConcurrentDictionary<ulong, Mp3ConverterService>();

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void OnLoad()
        {
            var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        public static void AddServices(ulong guildId, IMusicService service, Mp3ConverterService mp3Service)
        {
            if (!MusicServices.ContainsKey(guildId))
            {
                MusicServices.TryAdd(guildId, service);
            }

            if (!Mp3Services.ContainsKey(guildId))
            {
                Mp3Services.TryAdd(guildId, mp3Service);
            }
        }

        public static IMusicService GetService(ulong guildId)
        {
            if (!MusicServices.TryGetValue(guildId, out IMusicService service))
            {
                Log.Error("Server music service was not created, creating.");
                service = new FfmpegMusicService();
                MusicServices.TryAdd(guildId, service);
            }

            return service;
        }

        public static Mp3ConverterService GetMp3Service(ulong guildId)
        {
            if (!Mp3Services.TryGetValue(guildId, out Mp3ConverterService service))
            {
                Log.Error("Server mp3 service was not created, creating.");
                service = new Mp3ConverterService();
                Mp3Services.TryAdd(guildId, service);
            }

            return service;
        }

        public static void StopAll()
        {
            foreach (var musicService in MusicServices)
            {
                if (!musicService.Value.Stop())
                {
                    Log.Error($"Failed to stop music service for GuildId: {musicService.Key}");
                }
            }
        }
    }
}