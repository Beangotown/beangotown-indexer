using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace BeangoTown.Indexer.Plugin.Entities;

public class UserWeekRankIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string SeasonId { get; set; }

    [Keyword] public string CaAddress { get; set; }
    public int Week { get; set; }
    public long SumScore { get; set; }

    public int Rank { get; set; }

    public DateTime UpdateTime { get; set; }
}