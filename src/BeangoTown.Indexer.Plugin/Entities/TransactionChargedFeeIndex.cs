using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace BeangoTown.Indexer.Plugin.Entities;

public class TransactionChargedFeeIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string ChargingAddress { get; set; }
    [Keyword] public string Symbol { get; set; }
    public long Amount { get; set; }
}