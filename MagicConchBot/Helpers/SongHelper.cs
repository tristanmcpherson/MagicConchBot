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
        public static async Task<string> ParseUrlOrSearch(string query, GoogleApiInfoService service)
        {
            string url;
            var terms = query.Split(' ');
            if (!WebHelper.UrlRegex.IsMatch(query))
            {
                var firstTerm = terms.FirstOrDefault() ?? "";
                if (firstTerm == "yt")
                {
                    query = query.Replace(terms.First() + " ", string.Empty);
                }

                url = await service.GetFirstVideoByKeywordsAsync(query);
            }
            else
            {
                url = query;
            }

            return url;
        }

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
