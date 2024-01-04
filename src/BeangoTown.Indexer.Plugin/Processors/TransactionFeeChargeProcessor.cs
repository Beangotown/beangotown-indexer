using AElf.Contracts.MultiToken;
using AElfIndexer.Client;
using BeangoTown.Indexer.Plugin.Entities;

namespace BeangoTown.Indexer.Plugin.Processors;

using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.ObjectMapping;

public class TransactionFeeChargedProcessor : AElfLogEventProcessorBase<TransactionFeeCharged, TransactionInfo>
{
    private readonly ContractInfoOptions _contractInfoOptions;
    private readonly IObjectMapper _objectMapper;

    private readonly IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, TransactionInfo>
        _transactionFeeChargedInfoIndexRepository;

    public TransactionFeeChargedProcessor(ILogger<TransactionFeeChargedProcessor> logger,
        IOptionsSnapshot<ContractInfoOptions> contractInfoOptions,
        IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, TransactionInfo>
            transactionFeeChargedInfoIndexRepository,
        IObjectMapper objectMapper) : base(logger)
    {
        _contractInfoOptions = contractInfoOptions.Value;
        _objectMapper = objectMapper;
        _transactionFeeChargedInfoIndexRepository = transactionFeeChargedInfoIndexRepository;
    }

    public override string GetContractAddress(string chainId)
    {
        return _contractInfoOptions.ContractInfos.First(c => c.ChainId == chainId).TokenContractAddress;
    }

    protected override async Task HandleEventAsync(TransactionFeeCharged eventValue, LogEventContext context)
    {
        var chargeId = IdGenerateHelper.GenerateId(context.ChainId, context.TransactionId);

        var transactionFeeCharge = new TransactionChargedFeeIndex()
        {
            Id = chargeId
        };
        _objectMapper.Map(eventValue, transactionFeeCharge);
        transactionFeeCharge.ChargingAddress =
            AddressUtil.ToFullAddress(eventValue.ChargingAddress.ToBase58(), context.ChainId);
        _objectMapper.Map(context, transactionFeeCharge);
        await _transactionFeeChargedInfoIndexRepository.AddOrUpdateAsync(transactionFeeCharge);
    }
}