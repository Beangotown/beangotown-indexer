using AElf;
using AElf.Client.Extensions;
using AElf.CSharp.Core.Extension;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using BeangoTown.Indexer.Plugin.Processors;
using BeangoTown.Indexer.Plugin.Tests.Helper;
using Contracts.BeangoTownContract;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace BeangoTown.Indexer.Plugin.Tests.Processors;

public class BingoProcessorTests : BeangoTownIndexerPluginTestBase
{
    private const string chainId = "TDVW";
    private const string from = "2Pvmz2c57roQAJEtQ11fqavofdDtyD1Vehjxd7QRpQ7hwSqcF7";
    private const string blockHash = "dac5cd67a2783d0a3d843426c2d45f1178f4d052235a907a0d796ae4659103b1";
    private const string previousBlockHash = "e38c4fb1cf6af05878657cb3f7b5fc8a5fcfb2eec19cd76b73abb831973fbf4e";
    private const string transactionId = "c1e625d135171c766999274a00a7003abed24cfe59a7215aabf1472ef20a2da2";
    private const string to = "Lmemfcp2nB8kAvQDLxsLtQuHWgpH5gUWVmmcEkpJ2kRY9Jv25";
    private static long blockHeight = 100;
    private readonly IAElfIndexerClientInfoProvider _indexerClientInfoProvider;
    private readonly IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo> _gameInfoIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo>
        _rankSeasonIndexRepository;

    private readonly IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo>
        _userWeekRankIndexRepository;


    public BingoProcessorTests()
    {
        _gameInfoIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<GameIndex, TransactionInfo>>();

        _rankSeasonIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<RankSeasonConfigIndex, TransactionInfo>>();
        _userWeekRankIndexRepository =
            GetRequiredService<IAElfIndexerClientEntityRepository<UserWeekRankIndex, TransactionInfo>>();

    }

    
    public async Task HandlePlayProcessorAsync_Test()
    {
        var playProcessor = GetRequiredService<PlayProcessor>();
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

        var played = new Played
        {
            PlayBlockHeight = blockHeight,
            PlayId = HashHelper.ComputeFrom("PlayId"),
            PlayerAddress = from.ToAddress()
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(played.ToLogEvent());
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
                { "TransactionFee", "{\"ELF\":\"30000000\"}" },
                { "ResourceFee", "{\"ELF\":\"30000000\"}" }
            }
        };

        //step3: handle event and write result to blockStateSet
        await playProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);
        await Task.Delay(2000);

        //step5: check result
        var bingoGameIndexData = await _gameInfoIndexRepository.GetAsync(HashHelper.ComputeFrom("PlayId").ToHex());
        bingoGameIndexData.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleBingoProcessorAsync_Test()
    {
        var bingoProcessor = GetRequiredService<BingoProcessor>();
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

        var bingoed = new Bingoed
        {
            PlayBlockHeight = blockHeight,
            GridType = GridType.Blue,
            GridNum = 3,
            Score = 28,
            IsComplete = true,
            PlayId = HashHelper.ComputeFrom("PlayId"),
            BingoBlockHeight = blockHeight + 8,
            PlayerAddress = from.ToAddress(),
        };
        var logEventInfo = LogEventHelper.ConvertAElfLogEventToLogEventInfo(bingoed.ToLogEvent());
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
        await bingoProcessor.HandleEventAsync(logEventInfo, logEventContext);

        //step4: save blockStateSet into es
        await BlockStateSetSaveDataAsync<TransactionInfo>(blockStateSetKey);
        await Task.Delay(2000);

        //step5: check result
        var bingoGameIndexData = await _gameInfoIndexRepository.GetAsync(HashHelper.ComputeFrom("PlayId").ToHex());
        bingoGameIndexData.Score.ShouldBe(Convert.ToInt32(bingoed.Score));
        bingoGameIndexData.GridNum.ShouldBe(bingoed.GridNum);
        var rankSeason = await _rankSeasonIndexRepository.GetListAsync();
        rankSeason.Item1.ShouldBe(1);
        rankSeason.Item2[0].WeekInfos.Count.ShouldBe(1);
        var rankWeekUser = await _userWeekRankIndexRepository.GetListAsync();
        rankWeekUser.Item1.ShouldBe(1);
        rankWeekUser.Item2[0].SumScore.ShouldBe(bingoGameIndexData.Score);
    }


}