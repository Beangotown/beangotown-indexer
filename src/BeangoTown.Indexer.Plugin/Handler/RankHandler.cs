using AElf.CSharp.Core;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Handler;

public class RankHandler : BlockDataHandler
{
    private readonly GameInfoOption _gameInfoOption;
    private readonly ILogger<RankHandler> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IAElfIndexerClientEntityRepository<WeekRankTaskIndex, BlockInfo> _weekRankTaskIndexRepository;
    private readonly IAElfIndexerClientEntityRepository<UserWeekRankIndex, BlockInfo> _userWeekRankIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<UserSeasonRankIndex, BlockInfo>
        _userSeasonRankIndexRepository;

    private readonly RankInfoOption _rankInfoOption;
    private static long _curHeight = 0;
    private static readonly object LockObject = new object();
    private static DateTime _dateTime = DateTime.MinValue;

    public RankHandler(IClusterClient clusterClient,
        IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IDAppDataProvider dAppDataProvider, IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IAElfIndexerClientEntityRepository<WeekRankTaskIndex, BlockInfo> weekRankTaskIndexRepository,
        IAElfIndexerClientEntityRepository<UserWeekRankIndex, BlockInfo> userWeekRankIndexRepository,
        IAElfIndexerClientEntityRepository<UserSeasonRankIndex, BlockInfo> userSeasonRankIndexRepository,
        IOptionsSnapshot<GameInfoOption> gameInfoOption,
        IOptionsSnapshot<RankInfoOption> rankOption,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider, ILogger<RankHandler> logger) : base(clusterClient,
        objectMapper, aelfIndexerClientInfoProvider, dAppDataProvider, blockStateSetProvider,
        dAppDataIndexManagerProvider, logger)
    {
        _userWeekRankIndexRepository = userWeekRankIndexRepository;
        _weekRankTaskIndexRepository = weekRankTaskIndexRepository;
        _userSeasonRankIndexRepository = userSeasonRankIndexRepository;
        _gameInfoOption = gameInfoOption.Value;
        _rankInfoOption = rankOption.Value;
        _objectMapper = ObjectMapper;
        _logger = logger;
    }


    protected override async Task ProcessBlocksAsync(List<BlockInfo> data)
    {
        lock (LockObject)
        {
            if (data[0].BlockHeight < _curHeight.Add(_rankInfoOption.RankingBlockHeight))
            {
                return;
            }

            var now = DateTime.Now;
            if (now < _dateTime.Add(TimeSpan.FromSeconds(_rankInfoOption.RankingTimeSpan)))
            {
                return;
            }

            _curHeight = data[0].BlockHeight;
            _dateTime = now;
        }

        _logger.LogDebug("ProcessBlocksAsync showWeekNum {CurHeight} {Now}", _curHeight, _dateTime);
        
        var blockInfo = data[0];
        var seasonIndex = SeasonWeekUtil.ConvertRankSeasonIndex(_gameInfoOption);
        var rankWeekNum = SeasonWeekUtil.GetRankWeekNum(seasonIndex, blockInfo.BlockTime);
        var showWeekNum = SeasonWeekUtil.GetShowWeekNum(seasonIndex, blockInfo.BlockTime);
        bool isFinished = showWeekNum > rankWeekNum;
        if (!isFinished)
        {
            return;
        }

        var weekNum = Math.Max(showWeekNum, rankWeekNum);
        var isTaskSaved = await SaveRankWeekTaskAsync(blockInfo, seasonIndex, weekNum, isFinished);

        if (isTaskSaved) await SaveRankInfoAsync(seasonIndex.Id, weekNum, blockInfo, isFinished);

        var seasonTask =
            await _weekRankTaskIndexRepository.GetFromBlockStateSetAsync(seasonIndex.Id, blockInfo.ChainId);
        if (seasonTask != null && !seasonTask.IsFinished)
        {
            await RefreshSeasonRankAsync(blockInfo, seasonIndex.Id, _gameInfoOption.PlayerSeasonRankCount);
        }

        // if weekRanking is Finished,start seasonRanking 
        await SaveSeasonRankTaskAsync(isTaskSaved && isFinished, seasonIndex, blockInfo);
    }

    private async Task SaveSeasonRankTaskAsync(bool isFinished, RankSeasonConfigIndex seasonIndex, BlockInfo blockInfo)
    {
        if (isFinished)
        {
            var rankSeasonTask = new WeekRankTaskIndex
            {
                Id = seasonIndex.Id,
                SeasonId = seasonIndex.Id,
                IsFinished = false,
                TriggerTime = blockInfo.BlockTime
            };
            _objectMapper.Map(blockInfo, rankSeasonTask);
            await _weekRankTaskIndexRepository.AddOrUpdateAsync(rankSeasonTask);
        }
    }

    private async Task SaveRankInfoAsync(String seasonId, int weekNum, BlockInfo blockInfo, bool isFinished)
    {
        var rankCount = _gameInfoOption.PlayerWeekRankCount;
        if (isFinished)
        {
         
            var mustQuery = new List<Func<QueryContainerDescriptor<UserWeekRankIndex>, QueryContainer>>();

            mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(seasonId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Week).Value(weekNum)));

            QueryContainer Filter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

            var rankResult = await _userWeekRankIndexRepository.GetSortListAsync(Filter, null,
                sortFunc: s => s.Descending(a => a.SumScore).Ascending(a => a.UpdateTime),
                skip: 0,
                limit: rankCount);
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.Rank).GreaterThan(BeangoTownIndexerConstants.UserDefaultRank)));

            QueryContainer RankFilter(QueryContainerDescriptor<UserWeekRankIndex> f) => f.Bool(b => b.Must(mustQuery));

            _logger.LogDebug("rankResult.Item2.Count {Count}", rankResult.Item2.Count);
            for (var i = 0; i < rankResult.Item2.Count; i++)
            {
               
                var item = rankResult.Item2[i];
                item.Rank = i + 1;
                _objectMapper.Map(blockInfo, item);
                await _userWeekRankIndexRepository.AddOrUpdateAsync(item);
               
                await SaveSeasonUserRankAsync(blockInfo, item);
            }
        }
    }

    private async Task SaveSeasonUserRankAsync(BlockInfo blockInfo, UserWeekRankIndex item)
    {
        var rankSeasonUserId = IdGenerateHelper.GenerateId(item.SeasonId, AddressUtil.ToShortAddress(item.CaAddress));
        var rankSeasonUser =
            await _userSeasonRankIndexRepository.GetFromBlockStateSetAsync(rankSeasonUserId, blockInfo.ChainId);
        if (rankSeasonUser == null)
        {
            rankSeasonUser = new UserSeasonRankIndex
            {
                Id = rankSeasonUserId,
                SeasonId = item.SeasonId,
                CaAddress = item.CaAddress,
                SumScore = 0,
                Rank = BeangoTownIndexerConstants.UserDefaultRank
            };
        }

        rankSeasonUser.SumScore = Math.Max(rankSeasonUser.SumScore, item.SumScore);
        _objectMapper.Map(blockInfo, rankSeasonUser);
        await _userSeasonRankIndexRepository.AddOrUpdateAsync(rankSeasonUser);
    }

    private async Task<bool> SaveRankWeekTaskAsync(BlockInfo blockInfo, RankSeasonConfigIndex? seasonIndex, int weekNum,
        bool isFinished)
    {
        var rankWeekTaskId = IdGenerateHelper.GenerateId(seasonIndex.Id, weekNum);
        var rankWeekTask =
            await _weekRankTaskIndexRepository.GetFromBlockStateSetAsync(rankWeekTaskId, blockInfo.ChainId);
        _logger.LogDebug("ProcessBlocksAsync rankWeekTask {IsFinished}", rankWeekTask?.IsFinished);

        if (rankWeekTask != null && rankWeekTask.IsFinished)
        {
            return false;
        }

        rankWeekTask ??= new WeekRankTaskIndex
        {
            Id = rankWeekTaskId,
            SeasonId = seasonIndex.Id,
            Week = weekNum,
        };
        rankWeekTask.IsFinished = isFinished;
        rankWeekTask.TriggerTime = blockInfo.BlockTime;
        _objectMapper.Map(blockInfo, rankWeekTask);
        await _weekRankTaskIndexRepository.AddOrUpdateAsync(rankWeekTask);
        return true;
    }

    private async Task RefreshSeasonRankAsync(BlockInfo blockInfo, string seasonId, int rankCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserSeasonRankIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeasonId).Value(seasonId)));

        QueryContainer Filter(QueryContainerDescriptor<UserSeasonRankIndex> f) => f.Bool(b => b.Must(mustQuery));

        var needRankResult = await _userSeasonRankIndexRepository.GetListAsync(Filter, null, s => s.SumScore,
            SortOrder.Descending,
            skip: 0,
            limit: rankCount);
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Rank).GreaterThan(BeangoTownIndexerConstants.UserDefaultRank)));

        QueryContainer RankFilter(QueryContainerDescriptor<UserSeasonRankIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery));
        }

        var hasRankResult = await _userSeasonRankIndexRepository.GetListAsync(RankFilter);
        for (var i = 0; i < needRankResult.Item2.Count; i++)
        {
            var item = needRankResult.Item2[i];
            item.Rank = i + 1;
            _objectMapper.Map(blockInfo, item);
            await _userSeasonRankIndexRepository.AddOrUpdateAsync(item);
        }

        // del expired ranking
        var idList = needRankResult.Item2.Select(item => item.Id);
        var userSeasonRankIndices = hasRankResult.Item2.FindAll(item => !idList.Contains(item.Id));
        foreach (var notRankUser in userSeasonRankIndices)
        {
            notRankUser.Rank = BeangoTownIndexerConstants.UserDefaultRank;
            _objectMapper.Map(blockInfo, notRankUser);
            await _userSeasonRankIndexRepository.AddOrUpdateAsync(notRankUser);
        }

        // set seasonTask IsFinished true
        var rankSeasonTask = new WeekRankTaskIndex
        {
            Id = seasonId,
            SeasonId = seasonId,
            IsFinished = true,
            TriggerTime = blockInfo.BlockTime
        };
        _objectMapper.Map(blockInfo, rankSeasonTask);
        await _weekRankTaskIndexRepository.AddOrUpdateAsync(rankSeasonTask);
    }
}