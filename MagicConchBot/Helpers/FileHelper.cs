using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MagicConchBot.Helpers
{
    public static class FileHelper
    {
        public static async Task WaitForFile(string inFile, int size, CancellationToken token, int retries = 20, int sleep = 100)
        {
            var waitCount = 0;
            while (true)
            {
                var info = new FileInfo(inFile);
                if (info.Exists && info.Length >= size)
                    break;

                if (++waitCount == retries)
                    throw new Exception("Streaming file took too long to download. Stopping.");

                await Task.Delay(sleep, token);
            }
        }
    }
}
