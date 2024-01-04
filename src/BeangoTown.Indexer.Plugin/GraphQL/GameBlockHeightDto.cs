namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GameBlockHeightDto
{
    public long BingoBlockHeight { get; set; }
    public DateTime? BingoTime { get; set; }
    public string? LatestGameId { get; set; }
    public string? SeasonId { get; set; }

    public long GameCount { get; set; }
}