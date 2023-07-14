using System.Text.Json;
using System.Web;

namespace ScraperCollection.BestMp3Converter;

public static class BestMp3ConverterScraper {
    public static async Task<List<Mp3Option>> GetMp3Options(string youtubeUrl, CancellationToken cancellationToken = default) {
        var html = await HttpHelper.RequestHtml(
            "https://www.bestmp3converter.com/models/convertProcess.php",
            method: HttpMethod.Post,
            stringContent: $"type=mp3&search_txt={HttpUtility.UrlEncode(youtubeUrl)}",
            contentType: ContentType.Form,
            cancellationToken: cancellationToken
            );
        
        var title = html.DocumentNode.SelectSingleNode("//h5[@class='media-heading']")
            ?.InnerText.Trim()
            ?? "???";
        
        var duration = html.DocumentNode.SelectSingleNode("//span[@class='video_duration']")
            ?.InnerText.Trim()
            ?? "??:??";

        return html.DocumentNode.SelectNodes("//option")
            ?.Select(x => new Mp3Option(
                title,
                x.Attributes["data-size"].DeEntitizeValue,
                int.Parse(x.InnerText.Split(';')[^1][..^4]),
                duration,
                x.Attributes["data-hash"].DeEntitizeValue
            ))
            .ToList()
            ?? new();
    }

    public static async Task<Mp3Result> DownloadMp3(
        Mp3Option option,
        int donePollIntervalMs = 2000,
        CancellationToken cancellationToken = default)
    {
        var taskId = await HttpHelper.RequestString(
            "https://www.bestmp3converter.com/models/startTask.php",
            method: HttpMethod.Post,
            stringContent: $"hash={HttpUtility.UrlEncode(option.Hash)}",
            contentType: ContentType.Form,
            cancellationToken: cancellationToken
            );
        
        string downloadLink;
        while (true) {
            var taskStatus = await HttpHelper.RequestJson(
                "https://www.bestmp3converter.com/models/taskStatus.php",
                method: HttpMethod.Post,
                stringContent: $"taskId={HttpUtility.UrlEncode(taskId)}",
                contentType: ContentType.Form,
                cancellationToken: cancellationToken
                );
            
            var status = taskStatus["status"] ?? throw new JsonException("taskStatus missing status field");
            if (status.GetValue<string>() == "finished") {
                var link = taskStatus["download"] ?? throw new JsonException("taskStatus missing download field");
                downloadLink = link.GetValue<string>();
                break;
            }

            await Task.Delay(donePollIntervalMs, cancellationToken);
        }

        var stream = await HttpHelper.DownloadFile(downloadLink, cancellationToken);
        return new Mp3Result(option, stream);
    }
}