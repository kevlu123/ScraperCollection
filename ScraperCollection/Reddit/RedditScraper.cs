using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ScraperCollection.Reddit;

public static partial class RedditScraper {
    private static readonly ConcurrentDictionary<string, HttpResponseMessage> cache = new();
    private static readonly HttpClient httpClient = new(new HttpClientHandler {
        UseCookies = false,
        AutomaticDecompression = DecompressionMethods.All,
    });

    //public static async Task<Stream> Download(string url) {
    //    var response = await httpClient.GetAsync(url);
    //    return await response.Content.ReadAsStreamAsync();
    //}

    private static async Task<HttpResponseMessage> GetRequest(
        string url,
        bool compress = true,
        CancellationToken cancellationToken = default
        ) {
        if (cache.TryGetValue(url, out var cached)) {
            return cached;
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109) Gecko/20100101 Firefox/114.0");
        request.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
        request.Headers.Add("Accept-Encoding", compress ? "gzip;q=0.7, deflate;q=0.5, br;q=1.0, *;q=0.1" : "identity;q=1.0, *;q=0.1");
        request.Headers.Add("DNT", "1");
        request.Headers.Add("Cookie", "over18=1");
        request.Headers.Add("Sec-Fetch-Dest", "document");
        request.Headers.Add("Sec-Fetch-Mode", "navigate");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        var response = await httpClient.SendAsync(request, cancellationToken);

        cache[url] = response;
        return response;
    }

    private static GetPostsResult GetPostsFromHtml(string html) {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        if (doc.DocumentNode.SelectSingleNode(".//p[@id='noresults']") != null) {
            return new GetPostsResult {
                Status = SubredditStatus.Ok,
            };
        }

        var interstitial = doc.DocumentNode.SelectSingleNode(".//div[@class='interstitial']");
        if (interstitial != null) {
            var reasonTexts = interstitial.SelectNodes(".//p")
                .Select(x => x.InnerText);
            var reason = HtmlEntity.DeEntitize(string.Join("\n", reasonTexts));
            switch (interstitial.FirstChild.GetAttributeValue("alt", "")) {
                case "banned":
                    return new GetPostsResult {
                        Status = SubredditStatus.Banned,
                        BanReason = reason,
                    };
                case "private":
                    return new GetPostsResult {
                        Status = SubredditStatus.Private,
                        PrivateReason = reason,
                    };
            }
        }

        var postElems = doc.DocumentNode
            .SelectNodes("//div[contains(@class, 'thing')]")
            ?? (IEnumerable<HtmlNode>)Array.Empty<HtmlNode>();
        postElems = postElems
            .Where(x => !x.HasClass("promotedlink"));

        if (!postElems.Any()) {
            return new GetPostsResult {
                Status = SubredditStatus.Ok,
            };
        }

        var posts = new List<Post>();
        foreach (var p in postElems) {
            // Get post id
            var id = p.Id["thing_t3_".Length..];

            // Get subreddit
            var subreddit = p.GetAttributeValue("data-subreddit", "");

            // Get title
            var titleELem = p.SelectSingleNode(".//a[contains(@class, 'title')]");
            var title = HtmlEntity.DeEntitize(titleELem.InnerText);

            // Get thumbnail (full res, not actually thumbnail)
            var thumbnailUrl = "";
            var expando = p.SelectSingleNode(".//div[@data-cachedhtml]");
            var cachedHtml = expando?.GetAttributeValue("data-cachedhtml", null);
            if (cachedHtml != null) {
                cachedHtml = HtmlEntity.DeEntitize(cachedHtml);
                var subDoc = new HtmlDocument();
                subDoc.LoadHtml(cachedHtml);
                var a = subDoc.DocumentNode.SelectSingleNode(".//a");
                thumbnailUrl = a?.GetAttributeValue("href", "") ?? "";
            }

            // Backup method to get thumbnail
            if (thumbnailUrl.Length == 0) {
                var imageElem = p.SelectSingleNode(".//img");
                if (imageElem?.HasClass("awarding-icon") == false) {
                    thumbnailUrl = imageElem.GetAttributeValue("src", "");
                    if (thumbnailUrl.Length > 0) {
                        thumbnailUrl = HtmlEntity.DeEntitize(thumbnailUrl);
                        thumbnailUrl = "https:" + thumbnailUrl;
                    }
                }
            }

            // Get author
            var author = p.GetAttributeValue("data-author", "[deleted]");

            // Get upvote count
            var upvoteCount = p.SelectSingleNode(".//div[contains(@class, 'score')]")
                .NextSibling
                .InnerText;
            if (upvoteCount == "&bull;") {
                upvoteCount = "";
            }

            // Get comment count
            var commentCount = p.SelectSingleNode(".//a[contains(@class, 'comments')]")
                .InnerText;
            if (commentCount == "comment") {
                commentCount = "";
            } else {
                commentCount = commentCount.Split(' ')[0];
            }
            //var commentCount = p.GetAttributeValue("data-comments-count", "0");

            // Get post age and edit age
            var timeElements = p.SelectNodes(".//time");
            var postAge = timeElements[0].InnerText;
            var editAge = timeElements.Count > 1 ? timeElements[1].GetAttributeValue("title", "") : "";

            // Get if post is by moderator
            var byModerator = p.SelectSingleNode(".//a[contains(@class, 'moderator')]") != null;

            // Get raw/unraw content url and type
            var rcurl = p.GetAttributeValue("data-url", ""); // titleELem.GetAttributeValue("href", "");
            if (rcurl.StartsWith("/r/")) {
                rcurl = "https://www.reddit.com" + rcurl;
            }
            var curl = rcurl; // Default content url to raw content url

            var contentType = PostContentType.None;
            var IMAGE_EXT = new string[] { ".png", ".jpg", ".jpeg", ".gif" };
            if (rcurl.Contains("/r/") && rcurl.Contains("/comments/")) {
                contentType = PostContentType.Comments;
            } else if (rcurl.Contains("i.redd.it") || IMAGE_EXT.Any(x => rcurl.ToLower().EndsWith(x))) {
                contentType = PostContentType.Image;
            } else if (rcurl.Contains("v.redd.it")) {
                contentType = PostContentType.RedditVideo;
            } else if (rcurl.Contains("i.imgur.com") && rcurl.EndsWith(".gifv")) {
                curl = rcurl.Replace(".gifv", ".mp4");
                contentType = PostContentType.Video;
            } else if (rcurl.StartsWith("http")) {
                contentType = PostContentType.Link;
            }

            posts.Add(new Post(
                id: id,
                subreddit: subreddit,
                title: title,
                author: author,
                rawContentUrl: rcurl,
                contentUrl: curl,
                thumbnailUrl: thumbnailUrl,
                upvoteCount: upvoteCount,
                commentCount: commentCount,
                postAge: postAge,
                editAge: editAge,
                byModerator: byModerator,
                isMegathread: p.HasClass("linkflair-megathread"),
                isSticky: p.HasClass("stickied"),
                nsfw: p.HasClass("over18"),
                spoiler: p.HasClass("spoiler"),
                contentType: contentType
            ));
        }

        return new GetPostsResult {
            Status = SubredditStatus.Ok,
            Posts = posts,
        };
    }

    public static async Task<GetPostsResult> GetPosts(
        string subreddit,
        PostSorting sorting = PostSorting.Hot,
        string? after = null,
        CancellationToken cancellationToken = default
        ) {
        var link = new RedditLink();
        if (after != null) {
            link.Parameters.Add("after", $"t3_{after}");
        }

        switch (sorting) {
            case PostSorting.Hot:
                link.Directory = subreddit;
                break;
            case PostSorting.New:
                link.Directory = subreddit + "/new";
                break;
            case PostSorting.TopHour:
                link.Directory = subreddit + "/top";
                link.Parameters.Add("sort", "top");
                link.Parameters.Add("t", "hour");
                break;
            case PostSorting.TopDay:
                link.Directory = subreddit + "/top";
                link.Parameters.Add("sort", "top");
                link.Parameters.Add("t", "day");
                break;
            case PostSorting.TopWeek:
                link.Directory = subreddit + "/top";
                link.Parameters.Add("sort", "top");
                link.Parameters.Add("t", "week");
                break;
            case PostSorting.TopMonth:
                link.Directory = subreddit + "/top";
                link.Parameters.Add("sort", "top");
                link.Parameters.Add("t", "month");
                break;
            case PostSorting.TopYear:
                link.Directory = subreddit + "/top";
                link.Parameters.Add("sort", "top");
                link.Parameters.Add("t", "year");
                break;
            case PostSorting.TopAll:
                link.Directory = subreddit + "/top";
                link.Parameters.Add("sort", "top");
                link.Parameters.Add("t", "all");
                break;
            default:
                throw new InvalidOperationException();
        }

        var url = link.GetLink();
        var response = await GetRequest(url, cancellationToken: cancellationToken);

        if (response.RequestMessage?.RequestUri?.AbsolutePath.Contains("/subreddits/search") == true) {
            return new GetPostsResult {
                Status = SubredditStatus.NotFound,
            };
        }

        var html = await response.Content.ReadAsStringAsync(cancellationToken);
        return GetPostsFromHtml(html);
    }

    private static Comment GetCommentFromHtmlNode(HtmlNode node, string subreddit, string postId) {
        var c = node;

        string id;
        if (c.Id.Length > 0) {
            id = c.Id["thing_t1_".Length..];
        } else {
            id = c.GetAttributeValue("data-permalink", "").Split('/', StringSplitOptions.RemoveEmptyEntries).Last();
        }

        var author = c.SelectSingleNode(".//a[contains(@class, 'author')]")
            ?.InnerText
            ?? "[deleted]";

        var scoreNode = c.SelectSingleNode(".//span[contains(@class, 'score')]");
        if (scoreNode?.HasClass("score-hidden") == true) {
            scoreNode = null;
        }
        var upvoteCount = scoreNode
                ?.NextSibling
                ?.InnerText
                ?? "";
        //if (c.SelectSingleNode(".//span[contains(@class, 'score-hidden')]") == null) {
        //    upvoteCount = c.SelectSingleNode(".//span[contains(@class, 'score')]")
        //        .NextSibling
        //        .InnerText;
        //}

        var postAgeElem = c.SelectSingleNode(".//time");
        var postAge = postAgeElem?.InnerText ?? "";
        var editAgeElem = postAgeElem?.NextSibling;
        var editAge = editAgeElem?.Name == "time" ? editAgeElem.GetAttributeValue("title", "") : "";

        var text = c.SelectSingleNode(".//div[contains(@class, 'md')]")
            ?.InnerText.TrimEnd()
            ?? "";
        text = HtmlEntity.DeEntitize(text);

        var childRoot = c.SelectSingleNode(".//div[contains(@class, 'listing')]");

        var moreChildrenElem = childRoot?.LastChild?.PreviousSibling;
        string moreRepliesPostData = "";
        string moreRepliesCount = "";
        if (moreChildrenElem != null) {
            moreRepliesPostData = GetMoreRepliesPostData(moreChildrenElem, subreddit, postId, out moreRepliesCount);
        }

        var replies = childRoot?.FirstChild?.HasClass("morerecursion") == false
            ? GetCommentsFromHtmlNode(childRoot, subreddit, postId)
            : new();

        return new Comment(
            id: id,
            subreddit: subreddit,
            postId: postId,
            text: text,
            author: author,
            upvoteCount: upvoteCount,
            postAge: postAge,
            editAge: editAge,
            replies: replies,
            moreRepliesCount: moreRepliesCount,
            moreRepliesPostData: moreRepliesPostData
        );
    }

    private static List<Comment> GetCommentsFromHtmlNode(HtmlNode parent, string subreddit, string postId) {
        return parent
            .ChildNodes
            .Where(x => !x.HasClass("clearleft") && !x.HasClass("morechildren") && !x.HasClass("deleted"))
            .Select(x => GetCommentFromHtmlNode(x, subreddit, postId))
            .ToList();
    }

    public static async Task<GetCommentSectionResult> GetCommentSection(
        string subreddit,
        string postId,
        CancellationToken cancellationToken = default
        ) {
        var url = $"https://old.reddit.com/r/{subreddit}/comments/{postId}";
        var response = await GetRequest(url, cancellationToken: cancellationToken);
        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var text = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'top-matter')]")
            .NextSibling
            ?.FirstChild
            ?.FirstChild
            ?.NextSibling
            ?.InnerText
            ?? "";
        text = HtmlEntity.DeEntitize(text).Trim();
        text = ReduceBlankLinesRegex().Replace(text, "\n\n");

        var root = doc.DocumentNode.SelectSingleNode(".//div[contains(@class, 'nestedlisting')]");
        var comments = GetCommentsFromHtmlNode(root, subreddit, postId);

        string moreRepliesPostData = "";
        string moreRepliesCount = "";
        var lastChild = root.LastChild?.PreviousSibling;
        if (lastChild != null) {
            moreRepliesPostData = GetMoreRepliesPostData(lastChild, subreddit, postId, out moreRepliesCount);
        }

        return new GetCommentSectionResult(
            textContent: text,
            rootComments: comments,
            moreCommentsCount: moreRepliesCount,
            moreCommentsPostData: moreRepliesPostData
        );
    }

    public static async Task<Stream> GetRedditVideo(string dataUrl) {
        // reponse will never be null since the loop below will
        // have run at least once by the time it is used.
        HttpResponseMessage response = null!;
        string url;

        foreach (var res in new[] { 720, 480, 360, 240, 144 }) {
            url = $"{dataUrl}/DASH_{res}.mp4";
            response = await GetRequest(url, compress: false);
            if (response.IsSuccessStatusCode) {
                break;
            }
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    private static async Task<HttpResponseMessage> PostRequestMoreChildren(string data, CancellationToken cancellationToken = default) {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://old.reddit.com/api/morechildren");
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109) Gecko/20100101 Firefox/114.0");
        request.Headers.Add("Accept", "application/json, text/javascript, */*; q=0.01");
        request.Headers.Add("Accept-Language", "en-US,en;q=0.5");
        request.Headers.Add("Accept-Encoding", "gzip;q=0.7, deflate;q=0.5, br;q=1.0, *;q=0.1");
        request.Headers.Add("Origin", "https://old.reddit.com");
        request.Headers.Add("DNT", "1");
        request.Headers.Add("Cookie", "over18=1");
        request.Headers.Add("Sec-Fetch-Dest", "empty");
        request.Headers.Add("Sec-Fetch-Mode", "cors");
        request.Headers.Add("Sec-Fetch-Site", "same-origin");

        request.Content = new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static string GetMoreRepliesPostData(HtmlNode elem, string subreddit, string postId, out string count) {
        count = "";

        if (elem?.HasClass("morechildren") != true) {
            return "";
        }

        var aElem = elem.SelectSingleNode(".//a");
        var onclickJs = aElem?.GetAttributeValue("onclick", "") ?? "";
        var chunks = onclickJs.Split(", ");

        count = aElem?.SelectSingleNode(".//span")?.InnerText ?? "";
        count = HtmlEntity.DeEntitize(count).Trim();

        try {
            var sort = chunks[^3][1..^1];
            var moreRepliesIds = chunks[^2][1..^1].Split(',');
            var children = string.Join("%2C", moreRepliesIds);
            var first = moreRepliesIds[0];
            if (first.Contains(':')) {
                first = first.Split(':')[1];
            }
            return $"link_id=t3_{postId}&sort={sort}&children={children}&id=t1_{first}&limit_children=False&r={subreddit}&renderstyle=html";
        } catch (IndexOutOfRangeException) {
            return "";
        }
    }

    public static async Task<GetMoreRepliesResult> GetMoreReplies(string postData, string subreddit, string postId, CancellationToken cancellationToken = default) {
        var response = await PostRequestMoreChildren(postData, cancellationToken);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        var json = JsonNode.Parse(content)!;
        var commentData = json["jquery"]!.AsArray()
            .Where(x => x is JsonArray).Select(x => x!.AsArray())
            .Select(x => x.Last())
            .Where(x => x is JsonArray).Select(x => x!.AsArray())
            .Where(x => x.AsArray().Count == 2).Select(x => x[0])
            .Where(x => x is JsonArray).Select(x => x!.AsArray())
            .First()
            .Where(x => x is JsonObject).Select(x => x!["data"])
            .ToList();

        var comments = new List<Comment>();
        var parents = new Dictionary<string, Comment>();
        string rootMoreRepliesPostData = "";
        string rootMoreRepliesCount = "";
        foreach (var c in commentData) {
            var html = HtmlEntity.DeEntitize(c!["content"]!.ToString());
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var parentIdRaw = c["parent"]!.ToString();
            var parentId = parentIdRaw[3..];
            var firstChild = doc.DocumentNode.FirstChild;
            var moreRepliesPostData = GetMoreRepliesPostData(firstChild, subreddit, postId, out var moreRepliesCount);
            if (moreRepliesPostData.Length > 0) {
                if (parentIdRaw.StartsWith("t3_")) {
                    // More comments at root
                    rootMoreRepliesCount = moreRepliesCount;
                    rootMoreRepliesPostData = moreRepliesPostData;
                } else {
                    parents[parentId].MoreRepliesCount = moreRepliesCount;
                    parents[parentId].MoreRepliesPostData = moreRepliesPostData;
                }
                continue;
            }

            var comment = GetCommentFromHtmlNode(firstChild, subreddit, postId);

            if (parents.TryGetValue(parentId, out var parentComment)) {
                parentComment.Replies.Add(comment);
            } else {
                comments.Add(comment);
            }

            parents.Add(comment.Id, comment);
        }

        return new GetMoreRepliesResult(
            rootComments: comments,
            moreCommentsCount: rootMoreRepliesCount,
            moreCommentsPostData: rootMoreRepliesPostData
        );
    }

    [GeneratedRegex("\\n\\n\\n+")]
    private static partial Regex ReduceBlankLinesRegex();

    //public static async Task<Stream> GetVideo(string dataUrl) {
    //    var response = await SendRequest(dataUrl, true);
    //    response.EnsureSuccessStatusCode();
    //    return await response.Content.ReadAsStreamAsync();
    //}
}