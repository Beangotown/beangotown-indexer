using AElfIndexer.Client.GraphQL;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class BeangoTownIndexerPluginSchema : AElfIndexerClientSchema<Query>
{
    public BeangoTownIndexerPluginSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }
}