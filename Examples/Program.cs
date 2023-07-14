using ScraperCollection.BestMp3Converter;

static async Task BestMp3ConverterExample() {
    const string YOUTUBE_LINK = "https://www.youtube.com/watch?v=uDtgnYZsw7A";

    var options = await BestMp3Converter.GetMp3Options(YOUTUBE_LINK);
    if (options.Count == 0) {
        throw new Exception("No mp3 options found");
    }

    var lowestQuality = options.MinBy(x => x.Kbps)!;
    var result = await BestMp3Converter.DownloadMp3(lowestQuality);

    await result.Stream.CopyToAsync(File.Create("holydiver.mp3"));
}

static async Task RunExample(Func<Task> example) {
    try {
        await example();
    } catch (Exception ex) {
        Console.WriteLine(ex.Message);
    }
}

await RunExample(BestMp3ConverterExample);
