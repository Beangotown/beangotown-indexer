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
        serviceCollection.AddSingleton<IBlockChainDataHandler, RankHandler>();
        Configure<ContractInfoOptions>(configuration.GetSection("ContractInfo"));
        Configure<GameInfoOption>(configuration.GetSection("GameInfo"));
        Configure<RankInfoOption>(configuration.GetSection("RankInfo"));
    }

    protected override string ClientId => "AElfIndexer_BeangoTown";
    protected override string Version => "096d9d13252544c9875d3176d62f4374";
}