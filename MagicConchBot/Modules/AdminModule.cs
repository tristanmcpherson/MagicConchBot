﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using MagicConchBotApp.Attributes;
using MagicConchBotApp.Resources;

namespace MagicConchBotApp.Modules {
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

	[RequireBotOwner]
	[Group("blacklist")]
	public class Blacklist {
		[Command("add")]
		public Task AddToBlacklist(IUser user) {
			var config = Configuration.Load();
			var blacklist = new List<ulong>(config.Blacklist) {
					user.Id
				};

			config.Blacklist = blacklist.ToArray();
			config.Save();
			return Task.CompletedTask;
		}

		[Command("remove")]
		public Task RemoveFromBlacklist(IUser user) {
			var config = Configuration.Load();
			config.Blacklist = config.Blacklist.Where(u => u != user.Id).ToArray();
			config.Save();
			return Task.CompletedTask;
		}
	}
}