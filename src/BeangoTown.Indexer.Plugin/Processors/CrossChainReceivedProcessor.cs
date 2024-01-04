using Volo.Abp.ObjectMapping;
using AElf.Contracts.MultiToken;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BeangoTown.Indexer.Plugin.Processors;

public class CrossChainReceivedProcessor : AElfLogEventProcessorBase<CrossChainReceived, TransactionInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IUserBalanceProvider _balanceProvider;
    private readonly ILogger<AElfLogEventProcessorBase<CrossChainReceived, TransactionInfo>> _logger;

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.FirstOrDefault(c => c?.ChainId == chainId)
            ?.TokenContractAddress;
    }

    public CrossChainReceivedProcessor(IObjectMapper objectMapper, IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IUserBalanceProvider balanceProvider,
                                       ILogger<AElfLogEventProcessorBase<CrossChainReceived, TransactionInfo>> logger) : base(logger)
    {
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _balanceProvider = balanceProvider;
        _logger = logger;
    }

    protected override async Task HandleEventAsync(CrossChainReceived eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        _logger.LogDebug("CrossChainReceivedProcessor HandleEventAsync Symbol:{Symbol} TransactionId:{TransactionId}",
            eventValue.Symbol, context.TransactionId);
        // add 
        await _balanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(), eventValue.Amount,
            context);
    }
}