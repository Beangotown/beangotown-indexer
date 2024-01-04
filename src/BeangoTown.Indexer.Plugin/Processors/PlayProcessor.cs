using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using Contracts.BeangoTownContract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Processors;

public class PlayProcessor : BeangoTownProcessorBase<Played>
{
    private readonly IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> _gameInfoIndexRepository;
    private readonly ILogger<AElfLogEventProcessorBase<Played, TransactionInfo>> _logger;

    public PlayProcessor(
        ILogger<AElfLogEventProcessorBase<Played, TransactionInfo>> logger,
        IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> gameInfoIndexRepository,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IObjectMapper objectMapper
    ) : base(logger, objectMapper, contractInfoOptions)
    {
        _logger = logger;
        _gameInfoIndexRepository = gameInfoIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return ContractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).BeangoTownAddress;
    }

    protected override async Task HandleEventAsync(Played eventValue, LogEventContext context)
    {
        var oriGameIndex =
            await _gameInfoIndexRepository.GetFromBlockStateSetAsync(eventValue.PlayId.ToHex(), context.ChainId);
        if (oriGameIndex != null)
        {
            _logger.LogInformation("gameInfo exists {Id} ", eventValue.PlayId.ToHex());
            return;
        }

        var feeAmount = GetFeeAmount(context.ExtraProperties);
        var gameIndex = new GameIndex
        {
            Id = eventValue.PlayId.ToHex(),
            CaAddress = AddressUtil.ToFullAddress(eventValue.PlayerAddress.ToBase58(), context.ChainId),
            PlayBlockHeight = eventValue.PlayBlockHeight,
            PlayTransactionInfo = new TransactionInfoIndex
            {
                TransactionId = context.TransactionId,
                TriggerTime = context.BlockTime,
                TransactionFee = feeAmount
            }
        };
        ObjectMapper.Map(context, gameIndex);
        await _gameInfoIndexRepository.AddOrUpdateAsync(gameIndex);
    }
}