namespace ScraperCollection.BestMp3Converter;

public class Mp3Result {
    public string Title => option.Title;
    public string Size => option.Size;
    public int Kbps => option.Kbps;
    public string Duration => option.Duration;
    public string ThumbnailUrl => option.ThumbnailUrl;
    public Stream Stream { get; }

    private readonly Mp3Option option;

    internal Mp3Result(Mp3Option option, Stream stream) {
        this.option = option;
        Stream = stream;
    }
}