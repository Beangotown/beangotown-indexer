using AElfIndexer.Client;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains;
using AElfIndexer.Grains.Grain.Client;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using GraphQL;
using Nest;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class Query
{
    [Name("syncState")]
    public static async Task<SyncStateDto> SyncState(
        [FromServices] IClusterClient clusterClient, [FromServices] IAElfIndexerClientInfoProvider clientInfoProvider,
        [FromServices] IObjectMapper objectMapper, GetSyncStateDto dto)
    {
        var version = clientInfoProvider.GetVersion();
        var clientId = clientInfoProvider.GetClientId();
        var blockStateSetInfoGrain =
            clusterClient.GetGrain<IBlockStateSetInfoGrain>(
                GrainIdHelper.GenerateGrainId("BlockStateSetInfo", clientId, dto.ChainId, version));
        var confirmedHeight = await blockStateSetInfoGrain.GetConfirmedBlockHeight(dto.FilterType);
        return new SyncStateDto
        {
            ConfirmedBlockHeight = confirmedHeight
        };
    }
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

    [Name("getWeekRankRecords")]
    public static async Task<WeekRankRecordDto> GetWeekRankRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonRepository,
        [FromServices] IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserRepository,
        [FromServices] IObjectMapper objectMapper, GetWeekRankDto getWeekRankDto)
    {
        var rankRecordDto = new WeekRankRecordDto();
        var seasonIndex = await rankSeasonRepository.GetAsync(getWeekRankDto.SeasonId);
        if (seasonIndex == null)
        {
            return rankRecordDto;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(getWeekRankDto.SeasonId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Week).Value(getWeekRankDto.Week)));

        QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankWeekUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => Convert.ToInt64(a.SumScore)).Ascending(a => a.UpdateTime)
            , getWeekRankDto.MaxResultCount,
            getWeekRankDto.SkipCount);
        
        var rankDtos = new List<RankDto>();
        foreach (var item in result.Item2)
        {
            var rankDto = objectMapper.Map<UserWeekRankIndex, RankDto>(item);
            rankDtos.Add(rankDto);
        }

        rankRecordDto.RankingList = rankDtos;
        return rankRecordDto;
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
    
    [Name("getSeasonRankRecords")]
    public static async Task<SeasonRankRecordDto> GetSeasonRankRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<UserSeasonRankIndex, TransactionInfo> rankSeasonUserRepository,
        [FromServices] IObjectMapper objectMapper, GetSeasonRankDto getSeasonRankDto)
    {
        var rankRecordDto = new SeasonRankRecordDto();
        var seasonIndex = await rankSeasonRepository.GetAsync(getSeasonRankDto.SeasonId);
        if (seasonIndex == null)
        {
            return rankRecordDto;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<UserSeasonRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(getSeasonRankDto.SeasonId)));

        QueryContainer Filter(QueryContainerDescriptor<UserSeasonRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await rankSeasonUserRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.SumScore)
            , getSeasonRankDto.MaxResultCount,
            getSeasonRankDto.SkipCount);
        
        var rankDtos = new List<RankDto>();
        foreach (var item in result.Item2)
        {
            var rankDto = objectMapper.Map<UserSeasonRankIndex, RankDto>(item);
            rankDtos.Add(rankDto);
        }

        rankRecordDto.RankingList = rankDtos;

        return rankRecordDto;
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
        var now = DateTime.UtcNow;
        var mustQuery = new List<Func<QueryContainerDescriptor<RankSeasonConfigIndex>, QueryContainer>>();
        mustQuery.Add(q => q.DateRange(i => i.Field(f => f.RankBeginTime).LessThanOrEquals(now)));
        mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ShowEndTime).GreaterThanOrEquals(now)));

        QueryContainer Filter(QueryContainerDescriptor<RankSeasonConfigIndex> f) => f.Bool(b => b.Must(mustQuery));

        var rankSeason = await rankSeasonRepository.GetSortListAsync(Filter, null,
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

    [Name("getGameHistory")]
    public static async Task<GameHisResultDto> GetGameHistoryAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, LogEventInfo> gameIndexRepository,
        [FromServices]
        IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, LogEventInfo> transactionChargeFeeRepository,
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

        var gameResult = await gameIndexRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.BingoBlockHeight), getGameHisDto.MaxResultCount, getGameHisDto.SkipCount
        );
        if (!gameResult.Item2.IsNullOrEmpty())
        {
            var transactionIdList = gameResult.Item2.Where(game => game.BingoTransactionInfo?.TransactionFee > 0)
                .Select(game => game.BingoTransactionInfo?.TransactionId).ToList();
            var feeQuery = new List<Func<QueryContainerDescriptor<TransactionChargedFeeIndex>, QueryContainer>>();
            feeQuery.Add(q => q.Terms(i => i.Field(f => f.TransactionId).Terms(transactionIdList)));

            QueryContainer FeeFilter(QueryContainerDescriptor<TransactionChargedFeeIndex> f) =>
                f.Bool(b => b.Must(feeQuery));

            var transactionChargeFeeResult = await transactionChargeFeeRepository.GetListAsync(FeeFilter);
            if (!gameResult.Item2.IsNullOrEmpty())
            {
                var transactionChargeFeeMap = transactionChargeFeeResult.Item2.ToDictionary(
                    item => item.TransactionId,
                    item => item.ChargingAddress);
                foreach (var gameIndex in gameResult.Item2)
                {
                    var transactionId = gameIndex.BingoTransactionInfo?.TransactionId;
                    var address = "";
                    if (transactionId != null)
                    {
                        transactionChargeFeeMap.TryGetValue(transactionId, out address);
                        if (!gameIndex.CaAddress.Equals(address))
                        {
                            gameIndex.BingoTransactionInfo.TransactionFee = 0;
                        }
                    }
                }
            }
        }

        return new GameHisResultDto()
        {
            GameList = objectMapper.Map<List<GameIndex>, List<GameResultDto>>(gameResult.Item2)
        };
    }


    public static async Task<GameBlockHeightDto> GetLatestGameByBlockHeight(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameIndexRepository,
        [FromServices] IObjectMapper objectMapper, GetLatestGameDto getLatestGameHisDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.BingoBlockHeight).GreaterThanOrEquals(getLatestGameHisDto.BlockHeight)));
        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var result = await gameIndexRepository.GetSortListAsync(Filter, null,
            sortFunc: s => s.Descending(a => a.BingoBlockHeight), 1
        );
        GameBlockHeightDto gameBlockHeightDto = new GameBlockHeightDto();
        if (result.Item1 >= 1)
        {
            var latestGame = result.Item2[0];
            gameBlockHeightDto.BingoBlockHeight = latestGame.BingoBlockHeight;
            gameBlockHeightDto.SeasonId = latestGame.SeasonId;
            gameBlockHeightDto.LatestGameId = latestGame.Id;
            gameBlockHeightDto.BingoTime = latestGame.BingoTransactionInfo.TriggerTime;
            var countQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
            countQuery.Add(q => q.Term(i => i.Field(f => f.BingoBlockHeight).Value(latestGame.BingoBlockHeight)));
            QueryContainer CountFilter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(countQuery));
            var countResponse = await gameIndexRepository.CountAsync(CountFilter);
            gameBlockHeightDto.GameCount = countResponse.Count;
            return gameBlockHeightDto;
        }

        gameBlockHeightDto.BingoBlockHeight = getLatestGameHisDto.BlockHeight;
        return gameBlockHeightDto;

    }

    public static async Task<SeasonDto> GetSeasonConfigAsync(
        [FromServices] IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> repository,
        [FromServices] IObjectMapper objectMapper, GetSeasonDto getSeasonDto)
    {
        var result = await repository.GetAsync(getSeasonDto.SeasonId);
        return objectMapper.Map<RankSeasonConfigIndex, SeasonDto>(result);
    }
    
    [Name("getGoRecords")]
    public static async Task<List<GameRecordDto>> GetGoRecordsAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameRepository,
        [FromServices] IObjectMapper objectMapper, GetGoRecordDto getGoRecordDto)
    {
        var recordList = new List<GameRecordDto>();
        var skipCount = 0;
        Tuple<long, List<GameIndex>> result = null;
        do
        {
            result = await gameRepository.GetSortListAsync(null, null,
                sortFunc: s => s.Ascending(a => a.BingoTransactionInfo.TriggerTime)
                , GetGoRecordDto.MaxMaxResultCount, skipCount);

            foreach (var item in result.Item2)
            {
                if (recordList.Exists(i => i.CaAddress == item.CaAddress)) continue;
                var dto = new GameRecordDto();
                dto.Id = item.CaAddress;
                dto.CaAddress = item.CaAddress;
                dto.TriggerTime = item.BingoTransactionInfo.TriggerTime;
                recordList.Add(dto);
            }

            skipCount += GetGoRecordDto.MaxMaxResultCount;
        } while (result.Item2.Count >= GetGoRecordDto.MaxMaxResultCount);

        return recordList;
    }
    
    [Name("getGoCount")]
    public static async Task<GameGoCountDto> GetGoCountAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameRepository,
        GetGoDto getGoDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        if (getGoDto.CaAddressList != null)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CaAddress).Terms(getGoDto.CaAddressList)));
        }
        if (getGoDto.StartTime != null)
        {
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.BingoTransactionInfo.TriggerTime).GreaterThanOrEquals(getGoDto.StartTime)));
        }
        if (getGoDto.EndTime != null)
        {
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.BingoTransactionInfo.TriggerTime).LessThanOrEquals(getGoDto.EndTime)));
        }

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameList = new List<GameIndex>();
        var skipCount = getGoDto.SkipCount;
        Tuple<long, List<GameIndex>> result = null;
        do
        {
            result = await gameRepository.GetListAsync(Filter, skip: skipCount, limit: GetGoDto.MaxMaxResultCount);
            if (result.Item2.Count == 0) break;
            gameList.AddRange(result.Item2);
            skipCount += GetGoDto.MaxMaxResultCount;
        } while (result.Item2.Count >= GetGoDto.MaxMaxResultCount);

        var goResponse = gameList.GroupBy(g => g.CaAddress)
            .Select(group => new { caAddress = group.Key, Count = group.Count() })
            .Where(x => x.Count >= getGoDto.GoCount);

        return new GameGoCountDto
        {
            GoCount = goResponse.Count()
        };
    }

    [Name("getGameHistoryList")]
    public static async Task<GameHistoryResultDto> GetGameHistoryListAsync(
        [FromServices] IAElfIndexerClientEntityRepository<GameIndex, LogEventInfo> gameIndexRepository,
        [FromServices] IObjectMapper objectMapper, GetGameHistoryDto getGameHistoryDto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<GameIndex>, QueryContainer>>();
        if (!getGameHistoryDto.CaAddress.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CaAddress).Value(getGameHistoryDto.CaAddress)));
        }

        mustQuery.Add(q => q.DateRange(i =>
            i.Field(f => f.BingoTransactionInfo!.TriggerTime).GreaterThanOrEquals(getGameHistoryDto.BeginTime)));
        mustQuery.Add(q => q.DateRange(i =>
            i.Field(f => f.BingoTransactionInfo!.TriggerTime).LessThanOrEquals(getGameHistoryDto.EndTime)));

        QueryContainer Filter(QueryContainerDescriptor<GameIndex> f) => f.Bool(b => b.Must(mustQuery));

        var gameResult = await gameIndexRepository.GetListAsync(Filter, limit: getGameHistoryDto.MaxResultCount,
            skip: getGameHistoryDto.SkipCount
        );
        return new GameHistoryResultDto()
        {
            GameList = objectMapper.Map<List<GameIndex>, List<GameResultDto>>(gameResult.Item2)
        };
    }
    
    [Name("getUserBalanceList")]
    public static async Task<List<UserBalanceResultDto>> GetUserBalanceList(
        [FromServices] IAElfIndexerClientEntityRepository<UserBalanceIndex, LogEventInfo> repository,
        [FromServices] IObjectMapper objectMapper, GetUserBalanceDto userBalanceDto)
    {
        var symbols = userBalanceDto.symbols;
        if (symbols.IsNullOrEmpty())
        {
            return null;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(userBalanceDto.address)));
        
        mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(userBalanceDto.chainId)));
        
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Symbol).Terms(userBalanceDto.symbols)));

        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) => f.Bool(b => b.Must(mustQuery));
        
        var userBalanceIndexList = await repository.GetListAsync(Filter);

        return userBalanceIndexList.Item2.IsNullOrEmpty() ? new List<UserBalanceResultDto>() : objectMapper.Map<List<UserBalanceIndex>, List<UserBalanceResultDto>>(userBalanceIndexList.Item2);
    }
}