using System;
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

    public class NoFun : ModuleBase
    {
        [Command("nofun"), Alias("tristanhatesfun", "getfuckedidiot")]
        public async Task Retard()
        {
            await ReplyAsync($"Stop whining you absolute retard {Context.User.Mention}");
        }
    }

    /// TODO: Save these to config file
	[RequireBotOwner]
	[Group("admin blacklist")]
	public class Blacklist : ModuleBase {
		[Command("add")]
		public Task AddToBlacklist(IUser user) {

            //var config = Configuration.Load();
            var blacklist = new List<ulong>(Configuration.Blacklist) {
                    user.Id
                };

		    Configuration.Blacklist = blacklist.ToArray();
		    Environment.SetEnvironmentVariable(Constants.BlacklistVariable,
		        string.Join(',', blacklist.Select(x => x.ToString())));
            return Task.CompletedTask;
		}

		[Command("remove")]
		public Task RemoveFromBlacklist(IUser user) {
            var blacklist = Configuration.Blacklist.Where(u => u != user.Id).ToArray();

		    Configuration.Blacklist = blacklist.ToArray();

            Environment.SetEnvironmentVariable(Constants.BlacklistVariable,
		        string.Join(',', blacklist.Select(x => x.ToString())));
            return Task.CompletedTask;
		}
	}
}