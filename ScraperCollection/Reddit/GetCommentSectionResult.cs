namespace ScraperCollection.Reddit;

public class GetCommentSectionResult {
    public string TextContent { get; set; }
    public List<Comment> RootComments { get; set; }
    public string MoreCommentsCount { get; set; }
    public string MoreCommentsPostData { get; set; }

    public bool MoreRepliesAvailable => MoreCommentsPostData.Length > 0;

    public GetCommentSectionResult(
        string textContent,
        List<Comment> rootComments,
        string moreCommentsCount,
        string moreCommentsPostData
    ) {
        TextContent = textContent;
        RootComments = rootComments;
        MoreCommentsCount = moreCommentsCount;
        MoreCommentsPostData = moreCommentsPostData;
    }
}