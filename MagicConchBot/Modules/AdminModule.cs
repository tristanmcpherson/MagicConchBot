using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBot.Attributes;
using MagicConchBot.Resources;

namespace MagicConchBot.Modules {
	[RequireBotOwner]
	[Group("admin")]
	public class AdminModule : ModuleBase {
		[Command("reboot")]
		public async Task Reboot() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				await ReplyAsync("Rebooting bot host machine. Please wait approximately 1 minute, then ping using !info");

				Process.Start("./scripts/reboot.sh");
			}
		}

		[Command("restart")]
		public async Task RestartBot() {
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				await ReplyAsync("Restarting bot. Please wait.");

				Process.Start("./scripts/restart.sh");
			}
		}
	}

    /// TODO: Save these to config file
	[RequireBotOwner]
	[Group("admin blacklist")]
	public class Blacklist : ModuleBase {
		[Command("add")]
		public Task AddToBlacklist(IUser user) {
           
			//var config = Configuration.Load();
			//var blacklist = new List<ulong>(config.Blacklist) {
			//		user.Id
			//	};

			//config.Blacklist = blacklist.ToArray();
			//config.Save();
			return Task.CompletedTask;
		}

		[Command("remove")]
		public Task RemoveFromBlacklist(IUser user) {
			//config.Blacklist = config.Blacklist.Where(u => u != user.Id).ToArray();
			//config.Save();
			return Task.CompletedTask;
		}
	}
}