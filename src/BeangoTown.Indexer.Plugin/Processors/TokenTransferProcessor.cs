using AElf.Contracts.MultiToken;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Processors;

public class TokenTransferProcessor : AElfLogEventProcessorBase<Transferred, TransactionInfo>
{
    private readonly IObjectMapper _objectMapper;
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ILogger<AElfLogEventProcessorBase<Transferred, TransactionInfo>> _logger;

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).TokenContractAddress;
    }

    public TokenTransferProcessor(ILogger<AElfLogEventProcessorBase<Transferred, TransactionInfo>> logger, IObjectMapper objectMapper,
                                  IOptionsSnapshot<ContractInfoOptions> contractInfoOptions, IUserBalanceProvider userBalanceProvider) : base(logger)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _contractInfoOptions = contractInfoOptions.Value;
        _userBalanceProvider = userBalanceProvider;
    }

    protected override async Task HandleEventAsync(Transferred eventValue, LogEventContext context)
    {
        if (eventValue == null) return;
        if (context == null) return;
        await UpdateUserBalanceAsync(eventValue, context);
    }


    private async Task UpdateUserBalanceAsync(Transferred eventValue, LogEventContext context)
    {
        _logger.LogDebug("TokenTransferProcessor HandleEventAsync Symbol:{Symbol} From:{From},To:{To},TransactionId:{TransactionId}",
            eventValue.Symbol, eventValue.From.ToBase58(), eventValue.To.ToBase58(), context.TransactionId);
        // from reduce 
        await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol,
            eventValue.From.ToBase58(), -eventValue.Amount, context);

        // to add 
        await _userBalanceProvider.SaveUserBalanceAsync(eventValue.Symbol, eventValue.To.ToBase58(), eventValue.Amount, context);
    }
}