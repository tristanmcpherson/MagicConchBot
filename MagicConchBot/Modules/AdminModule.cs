using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Discord.Commands;
using MagicConchBot.Attributes;

namespace MagicConchBot.Modules
{
    [RequireBotOwner]
    [Group("admin")]
    public class AdminModule : ModuleBase
    {
        [Command("reboot")]
        public async Task Reboot()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await ReplyAsync("Rebooting bot host machine. Please wait approximately 1 minute, then ping using !info");
                Process.Start("reboot");
            }
        }

        [Command("restart")]
        public async Task RestartBot()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                await ReplyAsync("Restarting bot. Please wait.");
                Process.Start("./kill.sh && ./start.sh");
            }
        }
    }
}
