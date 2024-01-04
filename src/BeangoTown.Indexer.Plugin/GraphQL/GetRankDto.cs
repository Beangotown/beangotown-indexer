using Volo.Abp.Application.Dtos;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetRankDto : PagedResultRequestDto
{
    public string CaAddress { get; set; }
}