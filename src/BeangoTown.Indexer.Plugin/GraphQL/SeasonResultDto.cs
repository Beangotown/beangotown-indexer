namespace BeangoTown.Indexer.Plugin.GraphQL;

public class SeasonResultDto
{
    public List<SeasonDto> Season { get; set; }
}

public class SeasonDto
{
    public string Id { get; set; }
    public string? Name { get; set; }
    public int PlayerWeekRankCount { get; set; }
    public int PlayerWeekShowCount { get; set; }

    public int PlayerSeasonRankCount { get; set; }

    public int PlayerSeasonShowCount { get; set; }
    public DateTime RankBeginTime { get; set; }
    public DateTime RankEndTime { get; set; }
    public DateTime ShowBeginTime { get; set; }
    public DateTime ShowEndTime { get; set; }
    public List<WeekDto> WeekInfos { get; set; }
}

public class WeekDto
{
    public DateTime RankBeginTime { get; set; }
    public DateTime RankEndTime { get; set; }
    public DateTime ShowBeginTime { get; set; }
    public DateTime ShowEndTime { get; set; }
}