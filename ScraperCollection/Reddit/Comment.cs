namespace ScraperCollection.Reddit;

public class Comment {
    public string Id { get; set; }
    public string Subreddit { get; set; }
    public string PostId { get; set; }
    public string Text { get; set; }
    public string Author { get; set; }
    public string UpvoteCount { get; set; }
    public string PostAge { get; set; }
    public string EditAge { get; set; }
    public List<Comment> Replies { get; set; }
    public string MoreRepliesCount { get; set; }
    public string MoreRepliesPostData { get; set; }

    public int RecursiveChildCount => Replies.Count + Replies.Sum(reply => reply.RecursiveChildCount);
    public int RecursiveMoreRepliesLoadableCount => Replies.Sum(reply => reply.RecursiveMoreRepliesLoadableCount) + (MoreRepliesAvailable ? 1 : 0);
    public bool MoreRepliesAvailable => MoreRepliesPostData.Length > 0;
    public void ClearMoreReplies() => MoreRepliesPostData = "";

    public Comment(
        string id,
        string subreddit,
        string postId,
        string text,
        string author,
        string upvoteCount,
        string postAge,
        string editAge,
        List<Comment> replies,
        string moreRepliesCount,
        string moreRepliesPostData
    ) {
        Id = id;
        Subreddit = subreddit;
        PostId = postId;
        Text = text;
        Author = author;
        UpvoteCount = upvoteCount;
        PostAge = postAge;
        EditAge = editAge;
        Replies = replies;
        MoreRepliesCount = moreRepliesCount;
        MoreRepliesPostData = moreRepliesPostData;
    }
}
