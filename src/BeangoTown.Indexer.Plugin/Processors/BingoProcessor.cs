using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using Contracts.BeangoTownContract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Processors;

public class BingoProcessor : BeangoTownProcessorBase<Bingoed>
{
    private readonly IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> _gameInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo>
        _rankSeasonIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo>
        _rankWeekUserIndexRepository;

    private readonly GameInfoOption _gameInfoOption;
    private readonly ILogger<AElfLogEventProcessorBase<Bingoed, TransactionInfo>> _logger;

    public BingoProcessor(
        ILogger<AElfLogEventProcessorBase<Bingoed, TransactionInfo>> logger,
        IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameInfoIndexRepository,
        IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo> rankSeasonIndexRepository,
        IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo> rankWeekUserIndexRepository,
        IOptionsSnapshot<GameInfoOption> gameInfoOption,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IObjectMapper objectMapper
    ) : base(logger, objectMapper, contractInfoOptions)
    {
        _logger = logger;
        _gameInfoIndexRepository = gameInfoIndexRepository;
        _rankSeasonIndexRepository = rankSeasonIndexRepository;
        _rankWeekUserIndexRepository = rankWeekUserIndexRepository;
        _gameInfoOption = gameInfoOption.Value;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).BeangoTownAddress;
    }

    protected override async Task HandleEventAsync(Bingoed eventValue, LogEventContext context)
    {
        var seasonInfo = _gameInfoOption.SeasonInfo;
        RankSeasonConfigIndex seasonConfigRankIndex = null;
        if (!string.IsNullOrEmpty(seasonInfo.Id))
        {
            seasonConfigRankIndex = await SaveSeasonInfoAsync(context);
        }

        var gameIndex =
            await _gameInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.PlayId.ToHex(), context.ChainId);
        if (gameIndex == null)
        {
            _logger.LogWarning("gameInfo not exists {Id} ", eventValue.PlayId.ToHex());
            return;
        }

        var weekNum = SeasonWeekUtil.GetRankWeekNum(seasonConfigRankIndex, context.BlockTime);
        var seasonId = weekNum > -1 ? seasonConfigRankIndex.Id : null;
        await SaveGameIndexAsync(gameIndex, eventValue, context, seasonId);
        await SaveRankWeekUserIndexAsync(eventValue, context, weekNum, seasonId);
    }

    private async Task SaveRankWeekUserIndexAsync(Bingoed eventValue, LogEventContext context, int weekNum,
        string? seasonId)
    {
        if (weekNum > -1)
        {
            var rankWeekUserId = IdGenerateHelper.GenerateId(seasonId, weekNum, eventValue.PlayerAddress.ToBase58());
            var rankWeekUserIndex =
                await _rankWeekUserIndexRepository.GetFromBlockStateSetAsync(rankWeekUserId, context.ChainId);
            if (rankWeekUserIndex == null)
            {
                rankWeekUserIndex = new UserWeekRankIndex()
                {
                    Id = rankWeekUserId,
                    SeasonId = seasonId,
                    CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
                    Week = weekNum,
                    UpdateTime = context.BlockTime,
                    SumScore = eventValue.Score,
                    Rank = BeangoTownIndexerConstants.UserDefaultRank
                };
            }
            else
            {
                rankWeekUserIndex.SumScore += eventValue.Score;
                rankWeekUserIndex.UpdateTime = context.BlockTime;
            }

            ObjectMapper.Map(context, rankWeekUserIndex);
            await _rankWeekUserIndexRepository.AddOrUpdateAsync(rankWeekUserIndex);
        }
    }

    private async Task SaveGameIndexAsync(GameIndex gameIndex, Bingoed eventValue, LogEventContext context,
        string? seasonId)
    {
        var feeAmount = GetFeeAmount(context.ExtraProperties);
        
        gameIndex.SeasonId = seasonId;
        ObjectMapper.Map(eventValue, gameIndex);
        gameIndex.BingoTransactionInfo = new TransactionInfoIndex()
        {
            TransactionId = context.TransactionId,
            TriggerTime = context.BlockTime,
            TransactionFee = feeAmount
        };
        ObjectMapper.Map(context, gameIndex);
        await _gameInfoIndexRepository.AddOrUpdateAsync(gameIndex);
    }

    private async Task<RankSeasonConfigIndex> SaveSeasonInfoAsync(LogEventContext context)
    {
        var rankSeasonIndex = SeasonWeekUtil.ConvertRankSeasonIndex(_gameInfoOption);
        ObjectMapper.Map(context, rankSeasonIndex);
        await _rankSeasonIndexRepository.AddOrUpdateAsync(rankSeasonIndex);
        return rankSeasonIndex;
    }
}