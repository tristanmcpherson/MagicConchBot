﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using NLog;

namespace MagicConchBot.Services.Music
{
    public class HttpStreamingFileProvider : IFileProvider
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<string, Guid> _songIdDictionary;

        public HttpStreamingFileProvider()
        {
            _songIdDictionary = new ConcurrentDictionary<string, Guid>();
        }

        public async Task<string> GetStreamingFile(Song song)
        {
            if (!_songIdDictionary.TryGetValue(song.Url, out var guid))
            {
                guid = Guid.NewGuid();
                _songIdDictionary.TryAdd(song.Url, guid);
            }

            var directory = Path.Combine(Directory.GetCurrentDirectory(), "temp");
            var outputPath = Path.Combine(directory, $"{guid}.raw");

            try
            {
                // File exists but no way to verify file is not corrupted so delete
                Directory.CreateDirectory(directory);

                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to create download file.");
            }

            await Task.Factory.StartNew(async () =>
            {
                await WebHelper.ThrottledFileDownload(outputPath, song.StreamUri, song.Token);
            }, song.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return outputPath;
        }
    }
}
