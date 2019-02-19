using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NLog;

namespace MagicConchBot.Helpers
{
    public static class WebHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static readonly Regex UrlRegex =
            new Regex(@"(\b(https?):\/\/)?[-A-Za-z0-9+\/%?=_!.]+\.[-A-Za-z0-9+&#\/%=_]+");

        private const string GitHubRef =
            "https://api.github.com/repos/tristanmcpherson/MagicConchBot/git/refs/heads/dev";

        public static async Task<bool> UpToDateWithGitHub()
        {
            var gitHash = AppHelper.Version.Split('.').LastOrDefault() ?? "";

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
                httpClient.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue(new ProductHeaderValue("tristanmcpherson")));
                var head = await httpClient.GetStringAsync(GitHubRef);
                return JObject.Parse(head)["object"]["sha"].ToString().StartsWith(gitHash);
            }
        }

        public static async Task ThrottledFileDownload(string outputPath, string url, CancellationToken token,
            int bytesPerSecond = 1048576)
        {
            Log.Debug($"Starting to download file: {url}");

            var stopwatch = new Stopwatch();
            var totalBytes = 0;
            var buffer = new byte[4096];
            var retryCount = 0;
			if (File.Exists(outputPath)) {
				File.Delete(outputPath);
			}
            using (var outFile = new FileStream(outputPath, FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.ReadWrite))
            {
                using (var httpClient = new HttpClient())
                {
                    using (var stream = await httpClient.GetStreamAsync(url))
                    {
                        stopwatch.Start();
                        while (!token.IsCancellationRequested)
                        {
                            if (totalBytes > bytesPerSecond && stopwatch.ElapsedMilliseconds < 1000)
                            {
                                await Task.Delay(1000 - (int) stopwatch.ElapsedMilliseconds, token);
                                totalBytes = 0;
                            }
                            stopwatch.Restart();

                            var bytesDownloaded = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                            totalBytes += bytesDownloaded;

                            if (bytesDownloaded == 0)
                            {
                                if (++retryCount == 20)
                                    break;

                                await Task.Delay(50, token);
                            }

                            await outFile.WriteAsync(buffer, 0, bytesDownloaded, token);
                        }
                    }
                }
            }

            Log.Debug("Finished downloading file.");
        }
    }
}
