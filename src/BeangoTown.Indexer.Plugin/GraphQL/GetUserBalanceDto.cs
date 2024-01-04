namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetUserBalanceDto
{
    public string chainId { get; set; }
    public List<string> symbols { get; set; }
    public string address { get; set; }
}