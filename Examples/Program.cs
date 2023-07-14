using ScraperCollection.BestMp3Converter;
using ScraperCollection.Reddit;

static async Task BestMp3ConverterExample() {
    Console.WriteLine($"Running {nameof(BestMp3ConverterExample)}...");

    // Get the download options for this youtube video
    var options = await BestMp3ConverterScraper.GetMp3Options("https://www.youtube.com/watch?v=uDtgnYZsw7A");
    if (options.Count == 0) {
        throw new Exception("No mp3 options found.");
    }

    // Download the lowest quality mp3 (because why not)
    var lowestQuality = options.MinBy(x => x.Kbps)!;
    var result = await BestMp3ConverterScraper.DownloadMp3(lowestQuality);

    // Save the mp3 to a file. Prepend "ignore-" to the title so that git ignores it.
    var title = $"ignore-{result.Title}.mp3";
    await result.Stream.CopyToAsync(File.Create(title));

    Console.WriteLine($"Finished downloading {title}.");
}

static async Task RedditExample() {
    Console.WriteLine($"Running {nameof(RedditExample)}...");

    // Get the last post on the first page of r/pics sorted by hot
    var postsResult = await RedditScraper.GetPosts("pics");
    var firstPost = postsResult.Posts.Last();
    Console.WriteLine("Post:");
    Console.WriteLine($"Title:    {firstPost.Title}");
    Console.WriteLine($"Author:   {firstPost.Author}");
    Console.WriteLine($"Upvotes:  {firstPost.UpvoteCount}");
    Console.WriteLine($"Comments: {firstPost.CommentCount}");
    Console.WriteLine();

    // Get the first comment for the first post
    var commentsResult = await RedditScraper.GetCommentSection("pics", firstPost.Id);
    if (commentsResult.RootComments.Count == 0) {
        Console.WriteLine("This post has no comments.");
        return;
    }
    var firstComment = commentsResult.RootComments.First();
    Console.WriteLine("First comment:");
    Console.WriteLine($"Author:  {firstComment.Author}");
    Console.WriteLine($"Upvotes: {firstComment.UpvoteCount}");
    Console.WriteLine($"Text:    {firstComment.Text}");
}

static async Task RunExample(Func<Task> example) {
    try {
        await example();
    } catch (Exception ex) {
        Console.WriteLine(ex.Message);
    }
    Console.WriteLine();
}

await RunExample(BestMp3ConverterExample);
await RunExample(RedditExample);
