namespace BeangoTown.Indexer.Plugin;

public static class IdGenerateHelper
{
    public static string GenerateId(params object[] inputs)
    {
        return inputs.JoinAsString("-");
    }
    public static string GetUserBalanceId(string address, string chainId, string nftInfoId)
    {
        return GetId(address, chainId, nftInfoId);
    }
    
    public static string GetId(params object[] inputs)
    {
        return inputs.JoinAsString("-");
    }

}