namespace BeangoTown.Indexer.Plugin.GraphQL;

public class SeasonRankResultDto
{
    public string? SeasonName { get; set; }
    public int Status { get; set; }
    public DateTime? RefreshTime { get; set; }
    public List<RankDto>? RankingList { get; set; }
    public RankDto? SelfRank { get; set; }
}