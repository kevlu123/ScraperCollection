namespace ScraperCollection.Reddit;

public class GetPostsResult {
    public SubredditStatus Status { get; set; }
    public List<Post> Posts { get; set; } = new();
    public string BanReason { get; set; } = "";
    public string PrivateReason { get; set; } = "";
}