using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Helpers;
using Microsoft.Extensions.Caching.Memory;
using NLog;

namespace MagicConchBot.Services
{
    public record Mp3Request(string Name, string Url);

    public class Mp3ConverterService : IMp3ConverterService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private const string Folder = "Downloads";

        private readonly ConcurrentQueue<Mp3Request> _queueRequests;
        private readonly ConcurrentDictionary<Mp3Request, bool> _processingRequests;
        private readonly ConcurrentDictionary<Mp3Request, ConcurrentBag<IUser>> _recipients;

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _fileCache;
        private readonly SemaphoreSlim _semaphore;
        private Task _longRunningTask;

        public Mp3ConverterService(HttpClient httpClient, IMemoryCache fileCache)
        {
            _httpClient = httpClient;
            _fileCache = fileCache;
            _semaphore = new SemaphoreSlim(10);
            _queueRequests = new ConcurrentQueue<Mp3Request>();
            _recipients = new ConcurrentDictionary<Mp3Request, ConcurrentBag<IUser>>();
            _processingRequests = new ConcurrentDictionary<Mp3Request, bool>();


            try
            {
                if (Directory.Exists(Folder))
                {
                    Directory.Delete(Folder, true);
                }

                Directory.CreateDirectory(Folder);
            }
            catch { }
        }

        public void GetMp3(Mp3Request request, IUser user)
        {
            if (!_processingRequests.ContainsKey(request))
            {
                _queueRequests.Enqueue(request);
            }

            _recipients.GetOrAdd(request, _ => new ConcurrentBag<IUser>()).Add(user);
            EnsureLongRunningTask();
        }

        private void EnsureLongRunningTask()
        {
            if (_longRunningTask == null)
            {
                _longRunningTask = Task.Factory.StartNew(ProcessQueue, TaskCreationOptions.LongRunning);
            }
        }

        private async Task ProcessQueue()
        {
            while (true)
            {
                if (_queueRequests.IsEmpty)
                {
                    await Task.Delay(100);
                    continue;
                }

                var taskQueue = new List<Task>();

                while (_queueRequests.TryDequeue(out var request))
                {
                    await _semaphore.WaitAsync();

                    taskQueue.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await HandleMp3Request(request);

                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(taskQueue);
            }
        }

        private async Task HandleMp3Request(Mp3Request request)
        {
            _processingRequests.TryAdd(request, true);
            var filePath = await DownloadMp3(request);
            await SendMp3(request, filePath);
            if (filePath != null)
            {
                _fileCache
                    .GetOrCreate(request, (entry) =>
                    {
                        entry
                            .SetSlidingExpiration(TimeSpan.FromHours(1))
                            .RegisterPostEvictionCallback((_, value, _, _) => File.Delete(value as string));
                        return filePath;
                    });
            }

            _processingRequests.TryRemove(request, out var _);
        }

        private async Task SendMp3(Mp3Request request, string filePath)
        {
            if (_recipients.TryRemove(request, out var users))
            {
                await Task.WhenAll(
                    users.Select(async user =>
                    {
                        if (filePath == null)
                        {
                            await user.SendMessageAsync("Failed to download and convert file. Please try again later.");
                        }
                        else
                        {
                            await user.SendFileAsync(filePath);
                        }
                    })
                );
            }
        }

        public async Task<string> DownloadMp3(Mp3Request request)
        {
            try
            {
                if (_fileCache.TryGetValue(request, out var filePath))
                {
                    return filePath as string;
                }

                string sanitizedName = string.Concat(request.Name.Split(Path.GetInvalidFileNameChars()));

                var downloadFile = Path.Combine(Folder, sanitizedName + "_" + ".raw");
                var outputFile = Path.Combine(Folder, sanitizedName + "_" + ".mp3");

                var tokenSource = new CancellationTokenSource();
                // just let ffmpeg stream the file, no need to download it separately
                // on second thought, this allows us to stream the file down as to not saturate our download bandwidth
                Log.Info($"Downloading file for conversion: {request.Url}");
                //await WebHelper.ThrottledFileDownload(_httpClient, downloadFile, request.Url, tokenSource.Token);
                Log.Info($"Converting mp3: {outputFile}");

                var convert = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-re -i ""{request.Url}"" -vn -q:a 0 -y ""{outputFile}""",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                });

                if (convert == null)
                {
                    Log.Error("Couldn't start ffmpeg process.");
                    return null;
                }

                await convert.StandardOutput.ReadToEndAsync();
                await convert.WaitForExitAsync();

                File.Delete(downloadFile);
                return outputFile;
            }
            catch
            {
                return null;
            }
        }
    }
}