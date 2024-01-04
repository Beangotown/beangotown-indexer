using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Contracts.BeangoTownContract;
using Nest;

namespace BeangoTown.Indexer.Plugin.Entities;

public class GameIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CaAddress { get; set; }
    [Keyword] public string? SeasonId { get; set; }
    public long PlayBlockHeight { get; set; }
    public bool IsComplete { get; set; }
    public GridType GridType;
    public int GridNum;
    public int Score;
    public long BingoBlockHeight { get; set; }

    public TransactionInfoIndex? PlayTransactionInfo { get; set; }
    public TransactionInfoIndex? BingoTransactionInfo { get; set; }
}