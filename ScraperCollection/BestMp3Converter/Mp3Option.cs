namespace ScraperCollection.BestMp3Converter;

public class Mp3Option {
    public string Title { get; }
    public string Size { get; }
    public int Kbps { get; }
    public string Duration { get; }
    
    internal string Hash { get; }

    internal Mp3Option(string title, string size, int kbps, string duration, string hash) {
        Title = title;
        Size = size;
        Kbps = kbps;
        Duration = duration;
        Hash = hash;
    }
}