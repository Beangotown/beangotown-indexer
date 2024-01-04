using BeangoTown.Indexer.Plugin.Entities;

namespace BeangoTown.Indexer.Plugin;

public static class SeasonWeekUtil
{
    public static int GetRankWeekNum(RankSeasonConfigIndex? rankSeasonIndex, DateTime compareTime)
    {
        if (rankSeasonIndex != null)
        {
            int i = 0;
            foreach (var weekInfo in rankSeasonIndex.WeekInfos)
            {
                i++;
                if (compareTime.CompareTo(weekInfo.RankBeginTime) > -1 &&
                    weekInfo.RankEndTime.CompareTo(compareTime) > -1)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public static int GetShowWeekNum(RankSeasonConfigIndex? rankSeasonIndex, DateTime compareTime)
    {
        if (rankSeasonIndex != null)
        {
            int i = 0;
            foreach (var weekInfo in rankSeasonIndex.WeekInfos)
            {
                i++;
                if (compareTime.CompareTo(weekInfo.ShowBeginTime) > -1 &&
                    weekInfo.ShowEndTime.CompareTo(compareTime) > -1)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    public static int GetWeekNum(RankSeasonConfigIndex? rankSeasonIndex, DateTime compareTime)
    {
        return Math.Max(GetRankWeekNum(rankSeasonIndex, compareTime), GetShowWeekNum(rankSeasonIndex, compareTime));
    }

    public static void GetWeekStatusAndRefreshTime(RankSeasonConfigIndex? rankSeasonIndex, DateTime compareTime,
        out int status, out DateTime? refreshTime)
    {
        var showWeek = GetShowWeekNum(rankSeasonIndex, compareTime);
        var rankWeek = GetRankWeekNum(rankSeasonIndex, compareTime);
        if (rankWeek > -1)
        {
            status = 0;
            refreshTime = rankSeasonIndex.WeekInfos[rankWeek - 1].RankEndTime;
            return;
        }

        if (showWeek > -1)
        {
            if (rankSeasonIndex.WeekInfos.Count == showWeek)
            {
                status = 2;
                refreshTime = null;
                return;
            }

            status = 1;
            refreshTime = rankSeasonIndex.WeekInfos[showWeek].RankBeginTime;
            return;
        }

        status = 0;
        refreshTime = null;
    }

    public static void GetSeasonStatusAndRefreshTime(RankSeasonConfigIndex? rankSeasonIndex, DateTime compareTime,
        out int status, out DateTime? refreshTime)
    {
        var showWeek = GetShowWeekNum(rankSeasonIndex, compareTime);
        var rankWeek = GetRankWeekNum(rankSeasonIndex, compareTime);
        if (rankWeek > -1)
        {
            status = 0;
            refreshTime = rankSeasonIndex.WeekInfos[rankWeek - 1].ShowBeginTime;
            return;
        }

        if (showWeek > -1)
        {
            if (rankSeasonIndex.WeekInfos.Count == showWeek)
            {
                status = 1;
                refreshTime = null;
                return;
            }

            status = 0;
            refreshTime = rankSeasonIndex.WeekInfos[showWeek].ShowBeginTime;
            return;
        }

        status = 0;
        refreshTime = null;
    }

    public static RankSeasonConfigIndex ConvertRankSeasonIndex(GameInfoOption gameInfoOption)
    {
        var seasonInfo = gameInfoOption.SeasonInfo;
        RankSeasonConfigIndex rankSeasonConfigIndex = new RankSeasonConfigIndex
        {
            Id = seasonInfo.Id,
            Name = seasonInfo.Name,
            PlayerWeekRankCount = gameInfoOption.PlayerWeekRankCount,
            PlayerWeekShowCount = gameInfoOption.PlayerWeekShowCount,
            PlayerSeasonRankCount = gameInfoOption.PlayerSeasonRankCount,
            PlayerSeasonShowCount = gameInfoOption.PlayerSeasonShowCount,
            RankBeginTime = DateTimeHelper.ParseDateTimeByStr(seasonInfo.RankBeginTime),
            RankEndTime = DateTimeHelper.ParseDateTimeByStr(seasonInfo.RankEndTime),
            ShowBeginTime = DateTimeHelper.ParseDateTimeByStr(seasonInfo.ShowBeginTime),
            ShowEndTime = DateTimeHelper.ParseDateTimeByStr(seasonInfo.ShowEndTime),
        };
        List<RankWeekIndex> rankWeeks = new List<RankWeekIndex>();
        foreach (var weekInfo in seasonInfo.WeekInfos)
        {
            RankWeekIndex rankWeekIndex = new RankWeekIndex()
            {
                RankBeginTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.RankBeginTime),
                RankEndTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.RankEndTime),
                ShowBeginTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.ShowBeginTime),
                ShowEndTime = DateTimeHelper.ParseDateTimeByStr(weekInfo.ShowEndTime)
            };
            rankWeeks.Add(rankWeekIndex);
        }

        rankSeasonConfigIndex.WeekInfos = rankWeeks;
        return rankSeasonConfigIndex;
    }
}