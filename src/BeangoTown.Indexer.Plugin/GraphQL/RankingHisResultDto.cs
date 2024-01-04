namespace BeangoTown.Indexer.Plugin.GraphQL;

public class RankingHisResultDto
{
    public RankDto? Season { get; set; }
    public List<WeekRankDto>? Weeks { get; set; }
}