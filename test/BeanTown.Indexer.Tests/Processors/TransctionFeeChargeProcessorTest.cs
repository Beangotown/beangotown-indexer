using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using BeangoTown.Indexer.Plugin.Processors;
using BeangoTown.Indexer.Plugin.Tests.Helper;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;

namespace BeangoTown.Indexer.Plugin.Tests.Processors;

public class TransctionFeeChargeProcessorTest : BeangoTownIndexerPluginTestBase
{
    private const string chainId = "TDVW";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private static long blockHeight = 100;

    private readonly IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, TransactionInfo>
        _transactionFeeChargedInfoIndexRepository;

    public TransctionFeeChargeProcessorTest()
    {
        _transactionFeeChargedInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<TransactionChargedFeeIndex, TransactionInfo>>();
    }

    [Fact]
    public async Task HandleTransactionFeeProcessorAsync_Test()
    {
        var transactionFeeChargedLogEventProcessor = GetRequiredService<TransactionFeeChargedProcessor>();
        var blockStateSet = new BlockStateSet<TransactionInfo>
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            Confirmed = true,
            PreviousBlockHash = previousBlockHash
        };
        //step1: create blockStateSet
        var blockStateSetKey = await InitializeBlockStateSetAsync(blockStateSet, chainId);
        //step2: create  logEventInfo

        var transactionFeeCharged = new TransactionFeeCharged
        {
            ChargingAddress = Address.FromPublicKey("AAA".HexToByteArray()),
            Symbol = "ELF",
            Amount = 30000000
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(transactionFeeCharged.ToLogEvent());
        logEventInfo.BlockHeight = blockHeight;
        logEventInfo.ChainId = chainId;
        logEventInfo.BlockHash = blockHash;
        logEventInfo.PreviousBlockHash = previousBlockHash;
        logEventInfo.TransactionId = transactionId;
        var logEventContext = new LogEventContext
        {
            ChainId = chainId,
            BlockHeight = blockHeight,
            BlockHash = blockHash,
            PreviousBlockHash = previousBlockHash,
            TransactionId = transactionId,
            BlockTime = DateTime.Now,
            ExtraProperties = new Dictionary<string, string>
            {
                { "TransactionFee", "{\"ELF\":\"50000000\"}" },
                { "ResourceFee", "{\"ELF\":\"30000000\"}" }
            }
        };

        //step3: handle event and write result to blockStateSet
        await transactionFeeChargedLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);
        await Task.Delay(2000);

        var transactionChargeFee =
            await _transactionFeeChargedInfoIndexRepository.GetAsync(
                IdGenerateHelper.GenerateId(chainId, transactionId));
        transactionChargeFee.ChargingAddress.ShouldBe(
            AddressUtil.ToFullAddress(transactionFeeCharged.ChargingAddress.ToBase58(), chainId));
        transactionChargeFee.TransactionId.ShouldBe(transactionId);
        transactionChargeFee.Amount.ShouldBe(transactionFeeCharged.Amount);
        transactionChargeFee.Symbol.ShouldBe(transactionFeeCharged.Symbol);
        //step5: check result
    }
}