using AElf.Indexing.Elasticsearch;

namespace BeangoTown.Indexer.Plugin.Entities;

public class TransactionInfoIndex
{
    public string TransactionId { get; set; }

    public long TransactionFee { get; set; }

    public DateTime TriggerTime { get; set; }
}