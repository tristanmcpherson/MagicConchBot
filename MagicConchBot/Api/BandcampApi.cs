using System;
using MagicConchBot.Common.Types;
using HtmlAgilityPack;
using System.Threading.Tasks;

namespace MagicConchBot.Api
{
    public class BandcampApi
    {
        readonly string TrackNameSelector = "trackTitle";
        readonly string AlbumNameSelector = "fromAlbum";
        readonly string SongLengthSelector = "time_total";
        readonly string AlbumArtSelector = "popupImage";

        readonly Func<string, string> GetArtistNameSelector = (string url) => new Uri(url).GetLeftPart(UriPartial.Authority);

        public async Task<Song> GetSongInfo(string url)
        {
            var web = new HtmlWeb();
            var document = await web.LoadFromWebAsync(url);
            var host = GetArtistNameSelector(url);

            var track = document.DocumentNode.SelectSingleNode(@$"//*[@class=""{TrackNameSelector}""]").InnerText.Trim();
            var album = document.DocumentNode.SelectSingleNode(@$"//*[@class=""{AlbumNameSelector}""]").InnerText.Trim();
            var length = document.DocumentNode.SelectSingleNode(@$"//*[@class=""{SongLengthSelector}""]");
            var artist = document.DocumentNode.SelectSingleNode(@$"//*[@id=""name-section""]//*[@href=""{host}""]").InnerText.Trim();
            var albumArt = document.DocumentNode.SelectSingleNode($@"//*[@class=""{AlbumArtSelector}""]/@href").Attributes["href"].Value;

            return new Song($"{track} - {artist}", TimeSpan.Zero, url, albumArt, identifier: url);
        }
    }
}
