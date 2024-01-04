using Volo.Abp.Application.Dtos;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetGameHistoryDto : PagedResultRequestDto
{
    public string? CaAddress { get; set; }
    public DateTime BeginTime { get; set; }
    public DateTime EndTime { get; set; }
}