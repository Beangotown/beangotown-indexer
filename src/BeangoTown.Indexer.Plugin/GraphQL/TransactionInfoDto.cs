namespace BeangoTown.Indexer.Plugin.GraphQL;

public class TransactionInfoDto
{
    public string TransactionId { get; set; }

    public long TransactionFee { get; set; }

    public DateTime TriggerTime { get; set; }
}