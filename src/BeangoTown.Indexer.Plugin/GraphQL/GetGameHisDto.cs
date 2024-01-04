using Volo.Abp.Application.Dtos;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetGameHisDto : PagedResultRequestDto
{
    public string CaAddress { get; set; }
}