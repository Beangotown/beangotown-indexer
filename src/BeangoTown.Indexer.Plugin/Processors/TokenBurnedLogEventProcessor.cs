using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AElf.Contracts.MultiToken;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Processors;

public class TokenBurnedLogEventProcessor : AElfLogEventProcessorBase<Burned, TransactionInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly ILogger<AElfLogEventProcessorBase<Burned, TransactionInfo>> _logger;
    private readonly IUserBalanceProvider _userBalanceProvider;

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.TokenContractAddress;
    }

    public TokenBurnedLogEventProcessor(ILogger<AElfLogEventProcessorBase<Burned, TransactionInfo>> logger, IObjectMapper objectMapper,
                                        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IUserBalanceProvider userBalanceProvider) : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _userBalanceProvider = userBalanceProvider;
    }

    protected override async Task HandleEventAsync(Burned eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;

        _logger.LogDebug("TokenBurnedLogEventProcessor HandleEventAsync Symbol:{Symbol} TransactionId:{TransactionId}",
            eventValue.Symbol, context.TransactionId);

        // Burned reduce 
        await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.Burner.ToBase58(), -eventValue.Amount, context);
    }
}