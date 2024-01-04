using BeangoTown.Indexer.TestBase;
using Orleans.TestingHost;
using Volo.Abp.Modularity;

namespace BeangoTown.Indexer.Orleans.TestBase;

public abstract class BeangoTownIndexerOrleansTestBase<TStartupModule> : BeangoTownIndexerTestBase<TStartupModule>
    where TStartupModule : IAbpModule
{
    protected readonly TestCluster Cluster;

    public BeangoTownIndexerOrleansTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
    }
}