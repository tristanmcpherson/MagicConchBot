using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MagicConchBot.Services
{
    public class ChanService
    {
        private static readonly Regex YgylRegex = new Regex(@"ygyl|you groove you lose|you groove",
            RegexOptions.IgnoreCase);

        private static readonly Regex YouTubeRegex =
            new Regex(
                @"(?:https?:\/\/)?(?:www\.)?(?:youtu\.be\/|youtube\.com(?:\/embed\/|\/v\/|\/watch\?v=))(?<VideoId>[\w-]{10,12})(?:[\&\?]?t=)?(?<Time>[\d]+)?s?(?<TimeAlt>(\d+h)?(\d+m)?(\d+s)?)?");

        public async Task<List<string>> GetPostsWithVideosAsync(string boardName)
        {
            //var chan = await Chan.GetBoardAsync();
            //var videos = new List<string>();
            //var board = chan.Boards.First(b => b.BoardName == boardName);
            //for (var i = 1; i <= board.Pages; i++)
            //{
            //    var page = await Chan.GetThreadPageAsync(board.BoardName, i);
            //    foreach (var thread in page.Threads)
            //    {
            //        var t = thread.Posts.First();
            //        if (YgylRegex.IsMatch(t.Subject ?? string.Empty) || YgylRegex.IsMatch(t.Comment ?? string.Empty) ||
            //            YgylRegex.IsMatch(t.Name ?? string.Empty))
            //            foreach (var post in thread.Posts)
            //                if (post.HasImage && post.FileExtension == ".webm")
            //                {
            //                    var file = Constants.GetImageUrl(board.BoardName, post.FileName, post.FileExtension);
            //                    videos.Add(file);
            //                }
            //                else if (post.Comment.Contains("youtube.com"))
            //                {
            //                    var cleanedPost = post.Comment.Replace("<wbr>", string.Empty);
            //                    var match = YouTubeRegex.Match(cleanedPost);
            //                    videos.Add(match.Value);
            //                }
            //    }
            //}

            return null;
        }
    }
}