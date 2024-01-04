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
        serviceCollection.AddSingleton<IBlockChainDataHandler, RankHandler>();
        serviceCollection.AddSingleton<IBlockChainDataHandler, BeangoTownHandler>();
        Configure<ContractInfoOptions>(configuration.GetSection("ContractInfo"));
        Configure<GameInfoOption>(configuration.GetSection("GameInfo"));
        Configure<RankInfoOption>(configuration.GetSection("RankInfo"));
    }

    protected override string ClientId => "AElfIndexer_BeangoTown";
    protected override string Version => "0515ebd2b0914fddb300968394618d07";
}