using AutoMapper;

using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class CommonMappingProfile : Profile
{
    public CommonMappingProfile()
    {
        CreateMap(typeof(ProjectionQueryResult<>), typeof(ProjectionQueryResult<>));
        CreateMap<FacetStats, FacetStats>();
        CreateMap(typeof(QueryResultDocument<>), typeof(QueryResultDocument<>));
    }
}
