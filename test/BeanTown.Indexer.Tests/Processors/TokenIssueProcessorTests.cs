using AElf.Client.Extensions;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Processors;
using Shouldly;
using Xunit;

namespace BeangoTown.Indexer.Plugin.Tests.Processors;

public class TokenIssueProcessorTests: BeangoTownIndexerPluginTestBase
{
    private IUserBalanceProvider _userBalanceProvider;
    
    private const string chainId = "tDVW";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private static long blockHeight = 100;
    
    public TokenIssueProcessorTests()
    {
        _userBalanceProvider = GetService<IUserBalanceProvider>();
    }


    [Fact]
    public async Task HandleEventAsyncTest()
    {
        var tokenIssueLogEventProcessor = GetRequiredService<TokenIssueLogEventProcessor>();

        var logEventContext = MockLogEventContext(blockHeight);
        var blockStateSetKey = await MockTransactionBlockState(logEventContext);

        var Issued = new Issued
        {
            Symbol = "BEANPASS-Beango",
            Amount = 111111,
            To = to.ToAddress(),
        };
        var logEventInfo = MockLogEventInfo(Issued.ToLogEvent());
        
        await tokenIssueLogEventProcessor.HandleEventAsync(logEventInfo, logEventContext);


        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);
        await Task.Delay(2000);
        
        var userBalanceId = IdGenerateHelper.GetUserBalanceId(to, logEventContext.ChainId, "BEANPASS-Beango");

        var queryUserBalanceByIdAsync = await _userBalanceProvider.QueryUserBalanceByIdAsync(userBalanceId, logEventContext.ChainId);
        queryUserBalanceByIdAsync.Amount.ShouldBe(111111);
    }
}