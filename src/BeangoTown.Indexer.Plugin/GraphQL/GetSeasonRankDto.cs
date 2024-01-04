using Volo.Abp.Application.Dtos;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetSeasonRankDto : PagedResultRequestDto
{
    public string SeasonId { get; set; }
}