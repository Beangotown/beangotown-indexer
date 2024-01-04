using AElfIndexer.Client;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using BeangoTown.Indexer.Plugin.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace BeangoTown.Indexer.Plugin.Processors;

public class UserBalanceDto
{
    public string Symbol { get; set; }
    
    public long Amount { get; set; }
}



public class UserBalanceProvider : IUserBalanceProvider, ITransientDependency
{
    private readonly IAElfIndexerClientEntityRepository<UserBalanceIndex, TransactionInfo> _userBalanceIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<IUserBalanceProvider> _logger;
    private const string BeanGoTownCollectionSymbol = "BEANPASS-"; 
    
    public UserBalanceProvider(IObjectMapper objectMapper, IAElfIndexerClientEntityRepository<UserBalanceIndex, TransactionInfo> userBalanceIndexRepository, ILogger<IUserBalanceProvider> logger)
    {
        _objectMapper = objectMapper;
        _userBalanceIndexRepository = userBalanceIndexRepository;
        _logger = logger;
    }

    public async Task SaveUserBalanceAsync(string symbol, string address, long amount, LogEventContext context)
    {
        _logger.LogDebug("SaveUserBalanceAsync : symbol:{symbol},address:{address},amount:{amount}", symbol, address, amount);
        
        if (symbol.IsNullOrWhiteSpace() ||
            address.IsNullOrWhiteSpace() ||
            !symbol.StartsWith(BeanGoTownCollectionSymbol))
        {
            return;
        }
        
        var userBalanceId = IdGenerateHelper.GetUserBalanceId(address, context.ChainId, symbol);
        var userBalanceIndex =
            await _userBalanceIndexRepository.GetFromBlockStateSetAsync(userBalanceId, context.ChainId);
        if (userBalanceIndex == null)
        {
            userBalanceIndex = new UserBalanceIndex
            {
                Id = userBalanceId,
                ChainId = context.ChainId,
                Address = address,
                Amount = amount,
                Symbol = symbol,
                ChangeTime = context.BlockTime
            };
        }
        else
        {
            userBalanceIndex.Amount += amount;
            userBalanceIndex.ChangeTime = context.BlockTime;
        }

        _objectMapper.Map(context, userBalanceIndex);
        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
    }

    public async Task<List<UserBalanceDto>> QueryUserBalanceBySymbolsAsync(string chainId, List<string> symbols, string address)
    {
        var userBalanceDtoList = new List<UserBalanceDto>();
        if (symbols.IsNullOrEmpty())
        {
            return userBalanceDtoList;
        }

        if (chainId.IsNullOrWhiteSpace())
        {
            return userBalanceDtoList;
        }

        foreach (var symbol in symbols)
        {
            var userBalanceId = IdGenerateHelper.GetUserBalanceId(address, chainId, symbol);
            var userBalanceIndex = 
                await _userBalanceIndexRepository.GetFromBlockStateSetAsync(userBalanceId,chainId);
            var userBalanceDto = new UserBalanceDto
            {
                Amount = userBalanceIndex.Amount,
                Symbol = userBalanceIndex.Symbol
            };
            userBalanceDtoList.Add(userBalanceDto);
        }
        return userBalanceDtoList;
    }

    public async Task<UserBalanceIndex> QueryUserBalanceByIdAsync(string userBalanceId, string chainId)
    {
        if (userBalanceId.IsNullOrWhiteSpace() ||
            chainId.IsNullOrWhiteSpace())
        {
            return null;
        }
        return await _userBalanceIndexRepository.GetFromBlockStateSetAsync(userBalanceId,chainId);
    }
}

    