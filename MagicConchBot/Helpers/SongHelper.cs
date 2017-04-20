using System.Text;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Helpers
{
    public static class SongHelper
    {
        public static async Task DisplaySongsClean(Song[] songs, IMessageChannel channel)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < songs.Length; i++)
            {
                if (sb.Length > 1500)
                {
                    await channel.SendMessageAsync(sb.ToString());
                    sb.Clear();
                }

                sb.Append($"`{i + 1}` : {songs[i].GetInfo()}");
            }

            await channel.SendMessageAsync(sb.ToString());
        }
    }
}
