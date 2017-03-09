namespace MagicConchBot.Services
{
    using System.Collections.Concurrent;

    using log4net;

    using MagicConchBot.Common.Interfaces;

    public static class MusicServiceProvider
    {
        private static readonly ConcurrentDictionary<ulong, IMusicService> MusicServices = new ConcurrentDictionary<ulong, IMusicService>();

        private static readonly ILog Log = LogManager.GetLogger(typeof(MusicServiceProvider));

        public static void AddService(ulong guildId, IMusicService service)
        {
            if (!MusicServices.ContainsKey(guildId))
            {
                MusicServices.TryAdd(guildId, service);
            }
        }

        public static IMusicService GetService(ulong guildId)
        {
            if (!MusicServices.TryGetValue(guildId, out var service))
            {
                Log.Error("Server music service was not created, recreating.");
                service = new FfmpegMusicService();
                MusicServices.TryAdd(guildId, service);
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