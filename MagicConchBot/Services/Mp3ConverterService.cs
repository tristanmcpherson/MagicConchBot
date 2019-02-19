using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Resources;
using Microsoft.AspNetCore.Http.Internal;
using NLog;

namespace MagicConchBot.Services {
    public class Mp3ConverterService : IMp3ConverterService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static HttpClient _httpClient = new HttpClient();

        private static string _serverUrl;

        private readonly ConcurrentDictionary<string, Guid> _urlToUniqueFile;

        public Mp3ConverterService()
        {
            var config = Configuration.Load();

            _urlToUniqueFile = new ConcurrentDictionary<string, Guid>();
            _serverUrl = config.ServerMusicUrlBase;

            Recipients = new ConcurrentDictionary<IUser, bool>();
            GeneratingMp3 = new ConcurrentDictionary<string, bool>();
            Mp3Links = new ConcurrentDictionary<string, string>();
        }

        public ConcurrentDictionary<string, string> Mp3Links { get; private set; }
        public ConcurrentDictionary<string, bool> GeneratingMp3 { get; private set; }

        public ConcurrentDictionary<IUser, bool> Recipients { get; }

        public async Task GetMp3(Song song, IUser user)
        {
            Recipients.TryAdd(user, true);
            await GenerateMp3Async(song);
        }

        public async Task GenerateMp3Async(Song song)
        {
            if (GeneratingMp3.ContainsKey(song.Url))
            {
                return;
            }

            try
            {
                GeneratingMp3.TryAdd(song.Url, true);

                if (_urlToUniqueFile.TryGetValue(song.StreamUri, out Guid guid))
                {
                    var finalUrl = $"{_serverUrl}/api/upload/{guid.ToString().Replace("\"", "")}/";

                    await Task.WhenAll(Recipients.Select(async user =>
                        await user.Key.SendMessageAsync($"Here's your mp3!: {finalUrl}")));
                    return;
                }

                string sanitizedName = String.Concat(song.Name.Split(Path.GetInvalidFileNameChars()));

                var outputFile = sanitizedName + "_" + ".mp3";
                var downloadFile = sanitizedName + "_" + ".raw";

                var outputUrl = _serverUrl + Uri.EscapeDataString(outputFile);

                var tokenSource = new CancellationTokenSource();
                // just let ffmpeg stream the file, no need to download it separately
                // on second thought, this allows us to stream the file down as to not saturate our download bandwidth
                await WebHelper.ThrottledFileDownload(downloadFile, song.StreamUri, tokenSource.Token);

                var convert = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{downloadFile}"" -vn -ab 128k -ar 44100 -y ""{outputFile}""",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = false,
                    CreateNoWindow = true
                });

                if (convert == null)
                {
                    Log.Error("Couldn't start ffmpeg process.");
                    return;
                }

                convert.StandardOutput.ReadToEnd();
                convert.WaitForExit();

                using (var file = File.OpenRead(outputFile))
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(file), "file", outputFile);

                        var response = await _httpClient.PostAsync($"{_serverUrl}/api/upload", content, tokenSource.Token);
                        var fileId = await response.Content.ReadAsStringAsync();
                        var fileGuid = Guid.Parse(fileId.Replace("\"", ""));

                        _urlToUniqueFile.TryAdd(song.StreamUri, fileGuid);

                        var finalUrl = $"{_serverUrl}/api/upload/{fileId.Replace("\"", "")}/";

                        await Task.WhenAll(Recipients.Select(async user =>
                            await user.Key.SendMessageAsync($"Here's your mp3!: {finalUrl}")));
                        Recipients.Clear();
                    }
                }

                File.Delete(outputFile);
                File.Delete(downloadFile);

                GeneratingMp3.TryRemove(song.Url, out _);
            }
            catch (Exception)
            {
                await Task.WhenAll(Recipients.Select(async user => await user.Key.SendMessageAsync("Failed to get mp3 for song.")));
                GeneratingMp3.TryRemove(song.Url, out _);
                Recipients.Clear();
            }
        }
    }
}