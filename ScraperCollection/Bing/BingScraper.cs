using System.Web;
using HtmlAgilityPack;

namespace ScraperCollection.Bing;

public class BingSearchResult {
    public string Url { get; }
    public string Title { get; }
    public string Description { get; }

    public BingSearchResult(
        string url,
        string title,
        string description
    ) {
        Url = url;
        Title = title;
        Description = description;
    }
}

public static class BingScraper {
    public static async Task<List<BingSearchResult>> Search(string query, CancellationToken cancellationToken = default) {
        var url = $"https://www.bing.com/search?q={HttpUtility.UrlEncode(query)}";
        var html = await HttpHelper.RequestHtml(url, cancellationToken: cancellationToken);
        var htmlEntries = html.DocumentNode.SelectNodes("//ol[@id='b_results']/li[@class='b_algo']");

        var results = new List<BingSearchResult>();
        foreach (var htmlEntry in htmlEntries) {
            var linkElement = htmlEntry.SelectSingleNode(".//h2/a");
            var link = linkElement.GetAttributeValue("href", "");
            var title = linkElement.InnerText;

            var descriptionTexts = htmlEntry.SelectSingleNode(".//p[contains(@class,'b_algoSlug')]")
                ?.ChildNodes
                .Where(x => !x.HasClass("algoSlug_icon"))
                .Select(x => x.InnerText);
            var description = descriptionTexts == null
                ? ""
                : string.Join("", descriptionTexts);

            results.Add(new BingSearchResult(
                HtmlEntity.DeEntitize(link),
                HtmlEntity.DeEntitize(title),
                HtmlEntity.DeEntitize(description)
            ));
        }

        return results;
    }
}
