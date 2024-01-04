using Volo.Abp.Application.Dtos;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetGoDto : PagedResultRequestDto
{
    public string? ChainId { get; set; }
    public List<string>? CaAddressList { get; set; }
    public string? CaHash { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int GoCount { get; set; }
}

public class GetGoRecordDto : GetGoDto
{
}