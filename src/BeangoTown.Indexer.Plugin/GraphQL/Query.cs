using AElfIndexer.Client;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class Query
{
    public static async Task<SeasonResultDto> GetRankingSeasonList(
        [FromServices] IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> repository,
        [FromServices] IObjectMapper objectMapper)
    {
        var result = await repository.GetSortListAsync(null,
            sortFunc: s => s.Descending(a => Convert.ToInt64(a.Id))
        );

        return new SeasonResultDto
        {
            Season = objectMapper.Map<List<RankSeasonConfigIndex>, List<SeasonDto>>(result.Item2)
        };
    }

    public static async Task<WeekRankResultDto> GetWeekRank(
        [FromServices] IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserRepository,
        [FromServices] IObjectMapper objectMapper, GetRankDto getRankDto)
    {
        var rankResultDto = new WeekRankResultDto();
        var seasonIndex = await GetRankSeasonConfigIndexAsync(rankSeasonRepository);
        SeasonWeekUtil.GetWeekStatusAndRefreshTime(seasonIndex, DateTime.Now, out var status, out var refreshTime);
        rankResultDto.Status = status;
        rankResultDto.RefreshTime = refreshTime;
        int week = SeasonWeekUtil.GetWeekNum(seasonIndex, DateTime.Now);
        if (week == -1 || getRankDto.SkipCount >= seasonIndex.PlayerWeekShowCount)
        {
            return rankResultDto;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(seasonIndex.Id)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Week).Value(week)));

        QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankWeekUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => Convert.ToInt64(a.SumScore)).Ascending(a => a.UpdateTime)
            , seasonIndex.PlayerWeekRankCount,
            0);

        int rank = 0;
        List<RankDto> rankDtos = new List<RankDto>();
        foreach (var item in result.Item2)
        {
            var rankDto = objectMapper.Map<UserWeekRankIndex, RankDto>(item);
            rankDto.Rank = ++rank;
            rankDtos.Add(rankDto);
            if (rankDto.CaAddress.Equals(getRankDto.CaAddress))
            {
                rankResultDto.SelfRank = rankDto;
            }
        }

        if (getRankDto.SkipCount >= rankDtos.Count)
        {
            rankResultDto.RankingList = new List<RankDto>();
        }
        else
        {
            var count = Math.Min(rankDtos.Count - getRankDto.SkipCount,
                Math.Min(getRankDto.MaxResultCount, seasonIndex.PlayerWeekShowCount - getRankDto.SkipCount));
            rankResultDto.RankingList = rankDtos.GetRange(getRankDto.SkipCount, count);
        }
        if (rankResultDto.SelfRank == null)
        {
            var id = IdGenerateHelper.GenerateId(seasonIndex.Id, week, AddressUtil.ToShortAddress(getRankDto.CaAddress));
            var userWeekRankIndex = await rankWeekUserRepository.GetAsync(id);
            rankResultDto.SelfRank = ConvertWeekRankDto(objectMapper, getRankDto.CaAddress, userWeekRankIndex);
        }

        return rankResultDto;
    }

    public static async Task<SeasonRankResultDto> GetSeasonRank(
        [FromServices] IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<UserSeasonRankIndex, TransactionInfo> rankSeasonUserRepository,
        [FromServices] IObjectMapper objectMapper, GetRankDto getRankDto)
    {
        var rankResultDto = new SeasonRankResultDto();
        var seasonIndex = await GetRankSeasonConfigIndexAsync(rankSeasonRepository);
        SeasonWeekUtil.GetSeasonStatusAndRefreshTime(seasonIndex, DateTime.Now, out var status, out var refreshTime);
        rankResultDto.SeasonName = seasonIndex?.Name;
        rankResultDto.Status = status;
        rankResultDto.RefreshTime = refreshTime;
        if (seasonIndex == null || getRankDto.SkipCount >= seasonIndex.PlayerSeasonShowCount)
        {
            return rankResultDto;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UserSeasonRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(seasonIndex.Id)));

        QueryContainer Filter(QueryContainerDescriptor<UserSeasonRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankSeasonUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.SumScore)
            , Math.Min(getRankDto.MaxResultCount, seasonIndex.PlayerSeasonShowCount - getRankDto.SkipCount),
            getRankDto.SkipCount);
        int rank = getRankDto.SkipCount;
        List<RankDto> rankDtos = new List<RankDto>();
        foreach (var item in result.Item2)
        {
            var rankDto = objectMapper.Map<UserSeasonRankIndex, RankDto>(item);
            rankDto.Rank = ++rank;
            rankDtos.Add(rankDto);
            if (rankDto.CaAddress.Equals(getRankDto.CaAddress))
            {
                rankResultDto.SelfRank = rankDto;
            }
        }

        rankResultDto.RankingList = rankDtos;

        var id = IdGenerateHelper.GenerateId(seasonIndex.Id, AddressUtil.ToShortAddress(getRankDto.CaAddress));
        if (rankResultDto.SelfRank == null)
        {
            var userSeasonRankIndex = await rankSeasonUserRepository.GetAsync(id);
            rankResultDto.SelfRank = ConvertSeasonRankDto(objectMapper, getRankDto.CaAddress, userSeasonRankIndex);
        }

        return rankResultDto;
    }

    private static async Task<RankSeasonConfigIndex?> GetRankSeasonConfigIndexAsync(
        IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonRepository)
    {
        var rankSeason = await rankSeasonRepository.GetSortListAsync(null, null,
            sortFunc: s => s.Descending(a => Convert.ToInt64(a.Id))
            , 1, 0
        );
        if (rankSeason.Item2.Count == 0)
        {
            return null;
        }

        return rankSeason.Item2[0];
    }

    private static RankDto ConvertSeasonRankDto(IObjectMapper objectMapper, String caAddress,
        UserSeasonRankIndex? userSeasonRankIndex)
    {
        if (userSeasonRankIndex == null)
        {
            return new RankDto
            {
                CaAddress = caAddress,
                Score = 0,
                Rank = BeangoTownIndexerConstants.UserDefaultRank
            };
        }

        return objectMapper.Map<UserSeasonRankIndex, RankDto>(userSeasonRankIndex);
    }

    private static RankDto ConvertWeekRankDto(IObjectMapper objectMapper, String caAddress,
        UserWeekRankIndex? userWeekRankIndex)
    {
        if (userWeekRankIndex == null)
        {
            return new RankDto
            {
                CaAddress = caAddress,
                Score = 0,
                Rank = BeangoTownIndexerConstants.UserDefaultRank
            };
        }

        return objectMapper.Map<UserWeekRankIndex, RankDto>(userWeekRankIndex);
    }

    public static async Task<RankingHisResultDto> GetRankingHistory(
        [FromServices] IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> userRankWeekRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<UserSeasonRankIndex, TransactionInfo> userRankSeasonRepository,
        [FromServices] IObjectMapper objectMapper, GetRankingHisDto getRankingHisDto)
    {
        if (string.IsNullOrEmpty(getRankingHisDto.CaAddress) || string.IsNullOrEmpty(getRankingHisDto.SeasonId))
        {
            return new RankingHisResultDto();
        }

        var id = IdGenerateHelper.GenerateId(getRankingHisDto.SeasonId,
            AddressUtil.ToShortAddress(getRankingHisDto.CaAddress));
        var userSeasonRankIndex = await userRankSeasonRepository.GetAsync(id);
        var seasonRankDto = ConvertSeasonRankDto(objectMapper, getRankingHisDto.CaAddress, userSeasonRankIndex);
        var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(getRankingHisDto.SeasonId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getRankingHisDto.CaAddress)));

        QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await userRankWeekRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Ascending(a => a.Week)
        );
        if (result.Item2.Count > 0)
        {
            return new RankingHisResultDto()
            {
                Weeks = objectMapper.Map<List<UserWeekRankIndex>, List<WeekRankDto>>(result.Item2),
                Season = seasonRankDto
            };
        }

        return new RankingHisResultDto
        {
            Weeks = new List<WeekRankDto>(),
            Season = seasonRankDto
        };
    }

    public static async Task<GameHisResultDto> GetGameHistory(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameIndexRepository,
        [FromServices] IObjectMapper objectMapper, GetGameHisDto getGameHisDto)
    {
        if (string.IsNullOrEmpty(getGameHisDto.CaAddress))
        {
            return new GameHisResultDto()
            {
                GameList = new List<GameResultDto>()
            };
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getGameHisDto.CaAddress)));

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await gameIndexRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.BingoBlockHeight), getGameHisDto.MaxResultCount, getGameHisDto.SkipCount
        );
        return new GameHisResultDto()
        {
            GameList = objectMapper.Map<List<GameIndex>, List<GameResultDto>>(result.Item2)
        };
    }
}