using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace MagicConchBot.Helpers
{
    public static class WebHelper
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static async Task ThrottledFileDownload(string outputPath, string url, CancellationToken token,
            int bytesPerSecond = 1048576)
        {
            Log.Debug($"Starting to download file: {url}");

            var stopwatch = new Stopwatch();
            var totalBytes = 0;
            var buffer = new byte[4096];
            var retryCount = 0;
            using (var outFile = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
            using (var httpClient = new HttpClient())
            {
                using (var stream = await httpClient.GetStreamAsync(url))
                {
                    stopwatch.Start();
                    while (!token.IsCancellationRequested)
                    {
                        if (totalBytes > bytesPerSecond && stopwatch.ElapsedMilliseconds < 1000)
                        {
                            await Task.Delay((int) (1000 - stopwatch.ElapsedMilliseconds), token);
                            stopwatch.Restart();
                            totalBytes = 0;
                        }

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

            Log.Debug("Finished downloading file.");
        }
    }
}
