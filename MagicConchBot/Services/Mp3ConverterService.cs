namespace MagicConchBot.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    using Discord;

    using log4net;

    using MagicConchBot.Common.Types;
    using MagicConchBot.Resources;

    public class Mp3ConverterService
    {
        private static readonly ILog Log =
            LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static string serverPath;
        private static string serverUrl;

        private readonly ConcurrentDictionary<string, Guid> urlToUniqueFile;

        public Mp3ConverterService()
        {
            var config = Configuration.Load();

            urlToUniqueFile = new ConcurrentDictionary<string, Guid>();
            serverPath = config.ServerMusicPath;
            serverUrl = config.ServerMusicUrlBase;

            Recipients = new ConcurrentBag<IUser>();
            GeneratingMp3 = false;
        }

        public bool GeneratingMp3 { get; private set; }

        public ConcurrentBag<IUser> Recipients { get; }

        public async Task<string> GenerateMp3Async(Song song)
        {
            return await Task.Factory.StartNew(() =>
            {
                if (!urlToUniqueFile.TryGetValue(song.StreamUrl, out var guid))
                {
                    guid = Guid.NewGuid();
                    urlToUniqueFile.TryAdd(song.StreamUrl, guid);
                }

                var outputFile = song.Name + "_" + guid + ".mp3";
                var downloadFile = outputFile + ".raw";

                var outputUrl = serverUrl + Uri.EscapeDataString(outputFile);
                var destinationPath = Path.Combine(serverPath, outputFile);

                if (File.Exists(destinationPath))
                {
                    return outputUrl;
                }

                GeneratingMp3 = true;

                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(song.StreamUrl, downloadFile);
                }

                var convert = Process.Start(new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $@"-i ""{downloadFile}"" -vn -ar 44100 -ac 2 -ab 320k -f mp3 ""{outputFile}""",
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

                using (var source = File.OpenRead(outputFile))
                {
                    using (var destination = File.Create(destinationPath))
                    {
                        source.CopyTo(destination);
                    }
                }

                File.Delete(outputFile);
                File.Delete(downloadFile);

                GeneratingMp3 = false;

                return outputUrl;
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }
}
