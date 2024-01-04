using AElf.Contracts.MultiToken;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Processors;

public class TokenIssueLogEventProcessor : AElfLogEventProcessorBase<Issued, TransactionInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ILogger<AElfLogEventProcessorBase<Issued, TransactionInfo>> _logger;

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos?.FirstOrDefault(c => c?.ChainId == chainId)?.TokenContractAddress;
    }

    public TokenIssueLogEventProcessor(ILogger<AElfLogEventProcessorBase<Issued, TransactionInfo>> logger, IObjectMapper objectMapper, IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
                                       IUserBalanceProvider userBalanceProvider) : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _userBalanceProvider = userBalanceProvider;
    }


    protected override async Task HandleEventAsync(Issued eventValue, LogEventContext context)
    {
        if (eventValue == null || context == null) return;
        // Issued add 
        _logger.LogDebug("TokenIssueLogEventProcessor HandleEventAsync Symbol:{Symbol} TransactionId:{TransactionId}",
            eventValue.Symbol, context.TransactionId);
        
        await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(), eventValue.Amount,
            context);
    }
}