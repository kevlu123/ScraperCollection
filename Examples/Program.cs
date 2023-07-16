using ScraperCollection.BestMp3Converter;
using ScraperCollection.Reddit;
using ScraperCollection.Bing;
using ScraperCollection;

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

    // Save thumbnail to a file. Prepend "ignore-" to the title so that git ignores it.
    var thumbnailStream = await HttpHelper.DownloadFile(result.ThumbnailUrl);
    var thumbnailFilename = $"ignore-{result.Title}.jpg";
    using var thumbnailFile = File.Create(thumbnailFilename);
    await thumbnailStream.CopyToAsync(thumbnailFile);

    // Save the mp3 to a file
    var title = $"ignore-{result.Title}.mp3";
    using var file = File.Create(title);
    await result.Stream.CopyToAsync(file);

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

static async Task BingExample() {
    Console.WriteLine($"Running {nameof(BingExample)}...");

    // Get the first result for "the trooper"
    var results = await BingScraper.Search("the trooper");
    if (results.Count == 0) {
        Console.WriteLine("No results yielded.");
        return;
    }
    var firstResult = results.First();
    Console.WriteLine($"Title:       {firstResult.Title}");
    Console.WriteLine($"Description: {firstResult.Description}");
    Console.WriteLine($"Url:         {firstResult.Url}");
    Console.WriteLine();
}

static async Task RunExample(Func<Task> example) {
    try {
        await example();
    } catch (Exception ex) {
        Console.WriteLine(ex.Message);
    }
    Console.WriteLine();
}

await RunExample(BingExample);
await RunExample(RedditExample);
await RunExample(BestMp3ConverterExample);
