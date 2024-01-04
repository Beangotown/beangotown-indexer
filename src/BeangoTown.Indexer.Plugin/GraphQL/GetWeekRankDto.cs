using Volo.Abp.Application.Dtos;

namespace BeangoTown.Indexer.Plugin.GraphQL;

public class GetWeekRankDto : PagedResultRequestDto
{
    public string SeasonId { get; set; }
    public int Week { get; set; }
}