using AElf.CSharp.Core;
using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Client.Providers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Handler;

public class RankHandler : BlockDataHandler
{
    private readonly ILogger<RankHandler> _logger;

    public RankHandler(IClusterClient clusterClient,
        IObjectMapper objectMapper,
        IAElfIndexerClientInfoProvider aelfIndexerClientInfoProvider,
        IDAppDataProvider dAppDataProvider, IBlockStateSetProvider<BlockInfo> blockStateSetProvider,
        IDAppDataIndexManagerProvider dAppDataIndexManagerProvider, ILogger<RankHandler> logger) : base(clusterClient,
        objectMapper, aelfIndexerClientInfoProvider, dAppDataProvider, blockStateSetProvider,
        dAppDataIndexManagerProvider, logger)
    {
        _logger = logger;
    }


    protected override async Task ProcessBlocksAsync(List<BlockInfo> data)
    {
    }
}