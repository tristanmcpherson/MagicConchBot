using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using MagicConchBot.Common.Types;
using MagicConchBot.Services;

namespace MagicConchBot.Helpers
{
    public static class SongHelper
    {
        public static Task<string> ParseUrlOrSearch(string query, YoutubeInfoService service)
        {
            if (!WebHelper.UrlRegex.IsMatch(query))
            {
                return service.GetFirstVideoByKeywordsAsync(query);
            }

            return Task.FromResult(query);
        }

        public static async Task DisplaySongsClean(Song[] songs, IInteractionContext context)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < songs.Length; i++)
            {
                if (sb.Length > 1500)
                {
                    await context.Interaction.RespondAsync(sb.ToString());
                    sb.Clear();
                }

                sb.Append($"`{(i + 1).ToString().PadLeft((int)Math.Log(songs.Length, 10) + 1)}.` : {songs[i].GetInfo()}");
            }

            await context.Interaction.RespondAsync(sb.ToString());
        }
    }
}
