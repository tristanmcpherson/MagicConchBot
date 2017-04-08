using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Resources;
using NLog;

namespace MagicConchBot.Services
{
    public class Mp3ConverterService
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static string _serverPath;
        private static string _serverUrl;

        private readonly ConcurrentDictionary<string, Guid> _urlToUniqueFile;

        public Mp3ConverterService()
        {
            var config = Configuration.Load();

            _urlToUniqueFile = new ConcurrentDictionary<string, Guid>();
            _serverPath = config.ServerMusicPath;
            _serverUrl = config.ServerMusicUrlBase;

            Recipients = new ConcurrentBag<IUser>();
            GeneratingMp3 = false;
        }

        public bool GeneratingMp3 { get; private set; }

        public ConcurrentBag<IUser> Recipients { get; }

        public async Task<string> GenerateMp3Async(Song song)
        {
            if (!_urlToUniqueFile.TryGetValue(song.StreamUri, out Guid guid))
            {
                guid = Guid.NewGuid();
                _urlToUniqueFile.TryAdd(song.StreamUri, guid);
            }

            var outputFile = song.Name + "_" + guid + ".mp3";
            var downloadFile = song.Name + "_" + guid + ".raw";

            var outputUrl = _serverUrl + Uri.EscapeDataString(outputFile);
            var destinationPath = Path.Combine(_serverPath, outputFile);

            if (File.Exists(destinationPath))
                return outputUrl;

            GeneratingMp3 = true;

            var tokenSource = new CancellationTokenSource();
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
                return null;
            }

            convert.StandardOutput.ReadToEnd();
            convert.WaitForExit();

            File.Move(outputFile, destinationPath);

            File.Delete(outputFile);
            File.Delete(downloadFile);

            GeneratingMp3 = false;

            return outputUrl;
        }
    }
}