using AElf.Indexing.Elasticsearch;
using AElfIndexer.Client;
using Nest;

namespace BeangoTown.Indexer.Plugin.Entities;

public class WeekRankTaskIndex : AElfIndexerClientEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string SeasonId { get; set; }

    public int? Week { get; set; }

    public bool IsFinished { get; set; }

    public DateTime TriggerTime { get; set; }
}