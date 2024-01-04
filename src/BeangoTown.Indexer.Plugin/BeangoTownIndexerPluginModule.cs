using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.GraphQL;
using BeangoTown.Indexer.Plugin.Processors;
using BeangoTown.Indexer.Plugin.Handler;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace BeangoTown.Indexer.Plugin;

[DependsOn(typeof(AElfIndexerClientModule), typeof(AbpAutoMapperModule))]
public class BeangoTownIndexerPluginModule : AElfIndexerClientPluginBaseModule<BeangoTownIndexerPluginModule,
    BeangoTownIndexerPluginSchema, Query>
{
    protected override void ConfigureServices(IServiceCollection serviceCollection)
    {
        var configuration = serviceCollection.GetConfiguration();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, BingoProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, PlayProcessor>();
        serviceCollection.AddSingleton<IBlockChainDataHandler, BeangoTownHandler>();

        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, CrossChainReceivedProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TokenIssueLogEventProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TokenTransferProcessor>();
        serviceCollection.AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TokenBurnedLogEventProcessor>();

        Configure<ContractInfoOptions>(configuration.GetSection("ContractInfo"));
        Configure<GameInfoOption>(configuration.GetSection("GameInfo"));
        Configure<RankInfoOption>(configuration.GetSection("RankInfo"));
        serviceCollection
            .AddSingleton<IAElfLogEventProcessor<TransactionInfo>, TransactionFeeChargedProcessor>();
    }

    protected override string ClientId => "AElfIndexer_BeangoTown";
    protected override string Version => "6e4c7a05792e4ac9adbd86742b0d20b1";
}