using System.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using HtmlAgilityPack;
using System.Threading.Tasks;
using MediaWiki.NET;

namespace MagicConchBot.Services
{
    public class StardewValleyService
    {
        private readonly Wiki _wiki;

        public StardewValleyService()
        {
            _wiki = new Wiki("http://stardewvalleywiki.com/mediawiki");
        }

        public async Task<string> GetSummaryFromSearchAsync(string query, string sectionName = "")
        {
            var search = await _wiki.SearchExact(query);
            if (search == null)
                return null;

            var sections = await _wiki.GetSections(search.title);

            string sectionText;

            if (sectionName == "")
            {
                sectionText = await _wiki.GetHtmlPreview(search.title);
            }
            else
            {
                var section = sections.FirstOrDefault(s => string.Equals(s.line, sectionName, StringComparison.InvariantCultureIgnoreCase));
                if (section == null)
                    return null;
                var sectionNum = Convert.ToInt32(section.index);
                sectionText = await _wiki.GetHtmlSection(search.title, sectionNum);
            }

            var document = new HtmlDocument();
            document.LoadHtml(sectionText);

            var sb = new StringBuilder();

            if (sectionName == "Gifting")
            {
                var node = document.DocumentNode.SelectSingleNode("//table[@id='roundedborder']");

                var reactions = from reactionNode
                    in node.SelectNodes(@"//tr[position() > 1]")
                    select new VillagerReaction
                    {
                        Reaction = reactionNode.SelectSingleNode(".//th").InnerText.Trim(),
                        Villagers = reactionNode.SelectNodes(".//td/div/a[2]").Select(v => v.InnerText)
                    };

                foreach (var reaction in reactions)
                {
                    sb.Append(reaction + "\n\n");
                }
            }
            else
            {
                var nodes = document.DocumentNode.SelectNodes("//following-sibling::p");

                if (nodes == null)
                    throw new Exception($"Parsing exception. Query: {query}");

                foreach (var node in nodes)
                {
                    sb.Append(node.InnerText + "\n");
                }
            }

            if (sectionName == "")
            {
                sb.Append("See also: \n\n");
                foreach (var section in sections)
                {
                    sb.Append($"**#{section.line}**\n");
                }
            }

            return sb.ToString();
        }
    }

    public class VillagerReaction
    {
        public string Reaction;
        public IEnumerable<string> Villagers;

        public VillagerReaction()
        {
            Villagers = new List<string>();
        }

        public override string ToString()
        {
            return $"**{Reaction}**: \n" + string.Join(", ", Villagers);
        }
    }
}
