using AElf.CSharp.Core;
using AElfIndexer.Client.Handlers;
using AElfIndexer.Grains.State.Client;
using AutoMapper;
using BeangoTown.Indexer.Plugin.Entities;
using BeangoTown.Indexer.Plugin.GraphQL;
using Contracts.BeangoTownContract;
using Volo.Abp.AutoMapper;

namespace BeangoTown.Indexer.Plugin;

public class BeangoTownIndexerClientAutoMapperProfile : Profile
{
    public BeangoTownIndexerClientAutoMapperProfile()
    {
        CreateMap<LogEventContext, RankSeasonConfigIndex>();
        CreateMap<LogEventContext, GameIndex>();
        CreateMap<LogEventContext, UserWeekRankIndex>();
        CreateMap<BlockInfo, UserWeekRankIndex>().Ignore(destination => destination.Id);
        CreateMap<BlockInfo, UserSeasonRankIndex>().Ignore(destination => destination.Id);
        CreateMap<BlockInfo, WeekRankTaskIndex>().Ignore(destination => destination.Id);
        CreateMap<UserWeekRankIndex, RankDto>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => source.SumScore));

        CreateMap<UserWeekRankIndex, WeekRankDto>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => source.SumScore));
        CreateMap<UserSeasonRankIndex, RankDto>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => source.SumScore));
        CreateMap<GameIndex, GameResultDto>().ForMember(destination => destination.TranscationFee,
            opt => opt.MapFrom(source =>
                source.PlayTransactionInfo!.TransactionFee.Add(source.BingoTransactionInfo != null
                    ? source.BingoTransactionInfo.TransactionFee
                    : 0)));
        CreateMap<TransactionInfoIndex, TransactionInfoDto>();
        CreateMap<RankSeasonConfigIndex, SeasonDto>();
        CreateMap<RankWeekIndex, WeekDto>();
        CreateMap<Bingoed, GameIndex>().ForMember(destination => destination.Score,
            opt => opt.MapFrom(source => Convert.ToInt32(source.Score)));
    }
}