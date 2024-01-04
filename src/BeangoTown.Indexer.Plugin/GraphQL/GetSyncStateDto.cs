using AElfIndexer;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetSyncStateDto
{
    public string ChainId { get; set; }
    public BlockFilterType FilterType { get; set; }
}