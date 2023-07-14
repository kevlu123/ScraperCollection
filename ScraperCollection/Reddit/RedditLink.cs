namespace ScraperCollection.Reddit;

class RedditLink {
    public string Directory { get; set; } = "";
    public Dictionary<string, string> Parameters { get; } = new();

    public string GetLink() {
        var s = "https://old.reddit.com/r/"
            + Directory
            + "/";

        if (Parameters.Count > 0) {
            s += "?" + string.Join(
                "&",
                Parameters.Select(p => $"{p.Key}={p.Value}")
                );
        }

        return s;
    }
}