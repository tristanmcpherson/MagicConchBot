using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using Wiki.Net;

namespace MagicConchBot.Services
{
    public class StardewValleyService
    {
        //private readonly MediaWiki _wiki;

        public StardewValleyService()
        {
            //_wiki = new MediaWiki("http://stardewvalleywiki.com/mediawiki");
        }

        public async Task<string> GetSummaryFromSearchAsync(string query, string sectionName = "")
        {
			//var search = await _wiki.SearchExact(query);
			//if (search == null)
			//    return null;

			//var sections = await _wiki.GetSections(search.title);

			//string sectionText;

			//if (sectionName == "")
			//{
			//    sectionText = await _wiki.GetHtmlPreview(search.title);
			//}
			//else
			//{
			//    sectionName = sectionName.ToLowerInvariant();
			//    var section = sections.FirstOrDefault(s => s.line.ToLower() == sectionName);
			//    if (section == null)
			//        return null;

			//    var sectionNum = Convert.ToInt32(section.index);
			//    sectionText = await _wiki.GetHtmlSection(search.title, sectionNum);
			//}

			//var document = new HtmlDocument();
			//document.LoadHtml(sectionText);

			//var sb = new StringBuilder();

			//if (sectionName == "gifting")
			//{
			//    var node = document.DocumentNode.SelectSingleNode("//table[@id='roundedborder']");

			//    var reactions = from reactionNode
			//        in node.SelectNodes(@"//tr[position() > 1]")
			//        select new
			//        {
			//            Reaction = reactionNode.SelectSingleNode(".//th").InnerText.Trim(),
			//            Villagers = reactionNode.SelectNodes(".//td/div/a[2]").Select(v => v.InnerText)
			//        };

			//    foreach (var reaction in reactions)
			//        sb.Append($"**{reaction.Reaction}**: \n" + string.Join(", ", reaction.Villagers) + "\n\n");
			//}
			//else if (sectionName == "stages")
			//{
			//    var table = document.DocumentNode.SelectSingleNode("//table[@id='roundedborder']");

			//    var stages = table.SelectNodes(@".//tr[1]/th")
			//        .Zip(table.SelectNodes(@".//tr[3]/td"),
			//            (a, b) => new {Title = a.InnerText.Trim(), Stage = b.InnerText.Trim()});

			//    foreach (var stage in stages)
			//        sb.Append($"**{stage.Title}**: {stage.Stage}\n");
			//}
			//else
			//{
			//    var nodes = document.DocumentNode.SelectNodes("//following-sibling::p");

			//    if (nodes == null)
			//        throw new Exception($"Parsing exception. Query: {query}");

			//    foreach (var node in nodes)
			//        sb.Append(node.InnerText + "\n");
			//}

			//if (sectionName == "")
			//{
			//    sb.Append("See also: \n\n");
			//    foreach (var section in sections)
			//        sb.Append($"**#{section.line}**\n");
			//}

			//return sb.ToString();
			return null;
        }
    }
}