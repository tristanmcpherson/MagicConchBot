using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using MagicConchBot.Api;
using MagicConchBot.Common.Interfaces;
using MagicConchBot.Common.Types;

namespace MagicConchBot.Services
{
    public class BandcampResolveService : ISongInfoService
    {
        public BandcampResolveService()
        {
            BandcampApi = new BandcampApi();
        }

        public BandcampApi BandcampApi{ get; set; }

        //https://foisey.bandcamp.com/track/01twitb
        public Regex Regex { get; } = new Regex(@"(?:https?:\/\/)?.+\.bandcamp\.com\/track\/(?<trackId>[\w-]+)?",
            RegexOptions.IgnoreCase);

        public async Task<Song> GetSongInfoAsync(string url)
        {
            return await BandcampApi.GetSongInfo(url);
        }
    }
}