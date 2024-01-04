namespace BeangoTown.Indexer.Plugin;

public class ContractInfoOptions
{
    public List<ContractInfo> ContractInfos { get; set; }
}

public class ContractInfo
{
    public string ChainId { get; set; }
    public string BeangoTownAddress { get; set; }
    public string TokenContractAddress { get; set; }
}