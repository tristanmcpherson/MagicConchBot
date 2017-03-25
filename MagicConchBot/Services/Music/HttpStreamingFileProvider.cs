using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;
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
            var outputFile = Path.Combine(directory, $"{guid}.raw");

            try
            {
                // File exists but no way to verify file is not corrupted so delete
                Directory.CreateDirectory(directory);

                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to create download file.");
            }

            Log.Debug($"Starting to stream file: {song.StreamUri}");
            await Task.Factory.StartNew(async () =>
            {
                var stopwatch = new Stopwatch();
                var bytesPerSecond = 1048576;
                var totalBytes = 0;
                var buffer = new byte[4096];
                var retryCount = 0;
                using (
                    var outFile = new FileStream(outputFile, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
                using (var httpClient = new HttpClient())
                {
                    using (var stream = await httpClient.GetStreamAsync(song.StreamUri))
                    {
                        stopwatch.Start();
                        while (!song.TokenSource.IsCancellationRequested)
                        {
                            if (totalBytes > bytesPerSecond && stopwatch.ElapsedMilliseconds < 1000)
                            {
                                await Task.Delay((int)(1000 - stopwatch.ElapsedMilliseconds));
                                stopwatch.Restart();
                                totalBytes = 0;
                            }

                            var bytesDownloaded = await stream.ReadAsync(buffer, 0, buffer.Length,
                                song.TokenSource.Token);
                            totalBytes += bytesDownloaded;

                            if (bytesDownloaded == 0)
                            {
                                if (++retryCount == 20)
                                    break;

                                await Task.Delay(50);
                            }

                            await outFile.WriteAsync(buffer, 0, bytesDownloaded, song.TokenSource.Token);
                        }
                    }
                }
                Log.Debug("Finished downloading file.");
            }, song.TokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            return outputFile;
        }
    }
}
