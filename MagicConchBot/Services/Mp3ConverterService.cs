using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Types;
using MagicConchBot.Helpers;
using MagicConchBot.Resources;
using NLog;

namespace MagicConchBot.Services {
	public class Mp3ConverterService {
		private static readonly Logger Log = LogManager.GetCurrentClassLogger();

		private static string _serverPath;
		private static string _serverUrl;

		private readonly ConcurrentDictionary<string, Guid> _urlToUniqueFile;

		public Mp3ConverterService() {
			var config = Configuration.Load();

			_urlToUniqueFile = new ConcurrentDictionary<string, Guid>();
			_serverPath = config.ServerMusicPath;
			_serverUrl = config.ServerMusicUrlBase;

			Recipients = new ConcurrentDictionary<IUser, bool>();
			GeneratingMp3 = new ConcurrentDictionary<string, bool>();
			Mp3Links = new ConcurrentDictionary<string, string>();
		}

		public ConcurrentDictionary<string, string> Mp3Links { get; private set; }
		public ConcurrentDictionary<string, bool> GeneratingMp3 { get; private set; }

		public ConcurrentDictionary<IUser, bool> Recipients { get; }

		public async Task GetMp3(Song song, IUser user) {
			Recipients.TryAdd(user, true);
			await GenerateMp3Async(song);
		}

		public async Task GenerateMp3Async(Song song) {
			if (GeneratingMp3.ContainsKey(song.Url)) {
				return;
			}

			GeneratingMp3.TryAdd(song.Url, true);

			if (!_urlToUniqueFile.TryGetValue(song.StreamUri, out Guid guid)) {
				guid = Guid.NewGuid();
				_urlToUniqueFile.TryAdd(song.StreamUri, guid);
			}

			var outputFile = song.Name + "_" + guid + ".mp3";
			var downloadFile = song.Name + "_" + guid + ".raw";

			var outputUrl = _serverUrl + Uri.EscapeDataString(outputFile);
			var destinationPath = Path.Combine(_serverPath, outputFile);

			var tokenSource = new CancellationTokenSource();
			await WebHelper.ThrottledFileDownload(downloadFile, song.StreamUri, tokenSource.Token);

			var convert = Process.Start(new ProcessStartInfo {
				FileName = "ffmpeg",
				Arguments = $@"-i ""{downloadFile}"" -vn -ab 128k -ar 44100 -y ""{outputFile}""",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = false,
				CreateNoWindow = true
			});

			if (convert == null) {
				Log.Error("Couldn't start ffmpeg process.");
				return;
			}

			convert.StandardOutput.ReadToEnd();
			convert.WaitForExit();

			File.Move(outputFile, destinationPath);

			await Task.WhenAll(Recipients.Select(async user =>
				await user.Key.SendMessageAsync($"Here's your mp3!: {outputUrl}")));


			File.Delete(outputFile);
			File.Delete(downloadFile);

			GeneratingMp3.TryRemove(song.Url, out var _);
		}
	}
}