using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace BeangoTown.Indexer.Plugin.Tests.Helper;

public class SeasonWeekUtilTest : BeangoTownIndexerPluginTestBase

{
    private readonly GameInfoOption _gameInfoOption;

    public SeasonWeekUtilTest()
    {
        _gameInfoOption = GetRequiredService<IOptionsSnapshot<GameInfoOption>>().Value;
    }

    [Fact]
    public void SeasonWeekUtil_Test()
    {
        var seasonIndex = SeasonWeekUtil.ConvertRankSeasonIndex(_gameInfoOption);
        var week = SeasonWeekUtil.GetWeekNum(seasonIndex, DateTime.Now.AddDays(-1));
        week.ShouldBe(-1);
        SeasonWeekUtil.GetWeekStatusAndRefreshTime(seasonIndex, DateTime.Now.AddDays(-1), out var status,
            out var refreshTime);
        status.ShouldBe(0);
        refreshTime.ShouldBeNull();
        SeasonWeekUtil.GetWeekStatusAndRefreshTime(seasonIndex, DateTime.Now.AddDays(1), out status,
            out refreshTime);
        status.ShouldBe(0);
        refreshTime.ShouldNotBeNull();
        SeasonWeekUtil.GetWeekStatusAndRefreshTime(seasonIndex, DateTime.Now.AddDays(31), out status,
            out refreshTime);
        status.ShouldBe(2);
        refreshTime.ShouldBeNull();
        SeasonWeekUtil.GetSeasonStatusAndRefreshTime(seasonIndex, DateTime.Now.AddDays(1), out status,
            out refreshTime);
        status.ShouldBe(0);
        refreshTime.ShouldNotBeNull();
        SeasonWeekUtil.GetSeasonStatusAndRefreshTime(seasonIndex, DateTime.Now.AddDays(31), out status,
            out refreshTime);
        status.ShouldBe(1);
        refreshTime.ShouldBeNull();
    }
}