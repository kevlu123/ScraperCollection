namespace ScraperCollection.Reddit;

public class Post {
    public string Id { get; set; }
    public string Subreddit { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string RawContentUrl { get; set; }
    public string ContentUrl { get; set; }
    public string ThumbnailUrl { get; set; }
    public string UpvoteCount { get; set; }
    public string CommentCount { get; set; }
    public string PostAge { get; set; }
    public string EditAge { get; set; }
    public bool ByModerator { get; set; }
    public bool IsMegathread { get; set; }
    public bool IsSticky { get; set; }
    public bool Nsfw { get; set; }
    public bool Spoiler { get; set; }
    public PostContentType ContentType { get; set; }

    public string CommentsUrl => $"https://old.reddit.com/r/{Subreddit}/comments/{Id}";
    public string AuthorUrl => $"https://old.reddit.com/user/{Author}";
    //public string NextPageUrl => $"https://old.reddit.com/r/{Subreddit}/?&after=t3_{Id}";

    public Post(
        string id,
        string subreddit,
        string title,
        string author,
        string rawContentUrl,
        string contentUrl,
        string thumbnailUrl,
        string upvoteCount,
        string commentCount,
        string postAge,
        string editAge,
        bool byModerator,
        bool isMegathread,
        bool isSticky,
        bool nsfw,
        bool spoiler,
        PostContentType contentType
    ) {
        Id = id;
        Subreddit = subreddit;
        Title = title;
        Author = author;
        RawContentUrl = rawContentUrl;
        ContentUrl = contentUrl;
        ThumbnailUrl = thumbnailUrl;
        UpvoteCount = upvoteCount;
        CommentCount = commentCount;
        PostAge = postAge;
        EditAge = editAge;
        ByModerator = byModerator;
        IsMegathread = isMegathread;
        IsSticky = isSticky;
        Nsfw = nsfw;
        Spoiler = spoiler;
        ContentType = contentType;
    }
}