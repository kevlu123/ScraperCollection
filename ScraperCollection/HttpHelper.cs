using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using HtmlAgilityPack;

namespace ScraperCollection;

public static class ContentType {
    public const string Form = "application/x-www-form-urlencoded; charset=UTF-8";
}

public static class HttpHelper {
    private static readonly HttpClient httpClient = new(new HttpClientHandler {
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.All,
    });
    
    public static async Task<Stream> DownloadFile(string url, CancellationToken cancellationToken = default) {
        var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    public static async Task<HttpResponseMessage> Request(
        string url,
        IEnumerable<(string key, string value)>? headers = null,
        string? stringContent = null,
        string? contentType = null,
        HttpMethod? method = null,
        IEnumerable<(string key, string value)>? cookies = null,
        Dictionary<string, HttpResponseMessage>? cache = null,
        CancellationToken cancellationToken = default)
    {
        if (cache?.TryGetValue(url, out var cached) == true) {
            return cached;
        }

        var request = new HttpRequestMessage(method ?? HttpMethod.Get, url);
        if (headers != null) {
            foreach (var (key, value) in headers) {
                request.Headers.Add(key, value);
            }
        }
        
        if (cookies != null) {
            var cookieValue = string.Join(";", cookies.Select(x => $"{x.key}={x.value}"));
            request.Headers.Add("Cookie", cookieValue);
        }

        if (stringContent != null) {
            if (contentType == null) {
                throw new ArgumentNullException(nameof(contentType));
            }
            request.Content = new StringContent(stringContent);
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        }

        var response = await httpClient.SendAsync(request,  HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (cache != null) {
            cache[url] = response;
        }
        return response;
    }
    
    public static async Task<string> RequestString(
        string url,
        IEnumerable<(string key, string value)>? headers = null,
        string? stringContent = null,
        string? contentType = null,
        HttpMethod? method = null,
        IEnumerable<(string key, string value)>? cookies = null,
        Dictionary<string, HttpResponseMessage>? cache = null,
        CancellationToken cancellationToken = default)
    {
        var response = await Request(url, headers, stringContent, contentType, method, cookies, cache, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
    
    public static async Task<JsonNode> RequestJson(
        string url,
        IEnumerable<(string key, string value)>? headers = null,
        string? stringContent = null,
        string? contentType = null,
        HttpMethod? method = null,
        IEnumerable<(string key, string value)>? cookies = null,
        Dictionary<string, HttpResponseMessage>? cache = null,
        CancellationToken cancellationToken = default)
    {
        var str = await RequestString(url, headers, stringContent, contentType, method, cookies, cache, cancellationToken);
        return JsonNode.Parse(str)!; // Should never be null (probably)
    }
    
    public static async Task<HtmlDocument> RequestHtml(
        string url,
        IEnumerable<(string key, string value)>? headers = null,
        string? stringContent = null,
        string? contentType = null,
        HttpMethod? method = null,
        IEnumerable<(string key, string value)>? cookies = null,
        Dictionary<string, HttpResponseMessage>? cache = null,
        CancellationToken cancellationToken = default)
    {
        var html = await RequestString(url, headers, stringContent, contentType, method, cookies, cache, cancellationToken);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return doc;
    }
}
