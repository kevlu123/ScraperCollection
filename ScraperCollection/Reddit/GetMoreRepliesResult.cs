namespace ScraperCollection.Reddit;

public class GetMoreRepliesResult {
    public List<Comment> RootComments { get; set; } = new();
    public string MoreCommentsCount { get; set; }
    public string MoreCommentsPostData { get; set; }

    public bool MoreRepliesLoadable => MoreCommentsPostData.Length > 0;

    public GetMoreRepliesResult(
        List<Comment> rootComments,
        string moreCommentsCount,
        string moreCommentsPostData
    ) {
        RootComments = rootComments;
        MoreCommentsCount = moreCommentsCount;
        MoreCommentsPostData = moreCommentsPostData;
    }
}