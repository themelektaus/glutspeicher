namespace Glutspeicher.Server;

public struct ApiListInfo
{
    public int Total { get; set; }
    public TimeSpan Benchmark { get; set; }

    [JsonIgnore]
    public IList List { get; set; }
}
