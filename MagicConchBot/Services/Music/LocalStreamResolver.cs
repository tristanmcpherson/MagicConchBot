﻿using System.IO;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Resources;

namespace MagicConchBot.Services.Music
{
    public class LocalStreamResolver : ISongResolver
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<string> GetSongStreamUrl(MusicType musicType, string data)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (File.Exists(data))
                return new FileInfo(data).FullName;

            if (string.IsNullOrEmpty(Configuration.LocalMusicPath))
                return null;
            Directory.CreateDirectory(Configuration.LocalMusicPath);
            var file = Path.Combine(Configuration.LocalMusicPath, data);
            return File.Exists(file) ? file : null;
        }
    }
}
