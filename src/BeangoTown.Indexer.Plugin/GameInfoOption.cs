namespace BeangoTown.Indexer.Plugin;

public class GameInfoOption
{
    public int PlayerWeekShowCount { get; set; }
    public int PlayerWeekRankCount { get; set; }

    public int PlayerSeasonRankCount { get; set; }

    public int PlayerSeasonShowCount { get; set; }
    public SeasonInfo SeasonInfo { get; set; }
}

public class SeasonInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string RankBeginTime { get; set; }
    public string RankEndTime { get; set; }
    public string ShowBeginTime { get; set; }
    public string ShowEndTime { get; set; }
    public List<WeekInfo> WeekInfos { get; set; }
}

public class WeekInfo
{
    public string RankBeginTime { get; set; }
    public string RankEndTime { get; set; }
    public string ShowBeginTime { get; set; }
    public string ShowEndTime { get; set; }
}