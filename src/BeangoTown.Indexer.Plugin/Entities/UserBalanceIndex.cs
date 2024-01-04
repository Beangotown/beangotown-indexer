using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace BeangoTown.Indexer.Plugin.Entities;

public class UserBalanceIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    //userAccount Address
    [Keyword] public string Address { get; set; }
    
    public long Amount { get; set; }

    [Keyword] public string Symbol { get; set; }

    public DateTime ChangeTime { get; set; }
    
}