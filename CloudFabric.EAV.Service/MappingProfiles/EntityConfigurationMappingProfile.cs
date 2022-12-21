using AutoMapper;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.Projections;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class EntityConfigurationMappingProfile: Profile
{
    public EntityConfigurationMappingProfile()
    {
        CreateMap<EntityConfigurationCreateRequest, EntityConfiguration>();
        CreateMap<EntityConfigurationUpdateRequest, EntityConfiguration>();

        CreateMap<EntityConfiguration, EntityConfigurationViewModel>();

        CreateMap<EntityConfigurationProjectionDocument, EntityConfigurationViewModel>();
        CreateMap<ProjectionQueryResult<EntityConfigurationProjectionDocument>, ProjectionQueryResult<EntityConfigurationViewModel>>();
        CreateMap<QueryResultDocument<EntityConfigurationProjectionDocument>, QueryResultDocument<EntityConfigurationViewModel>>();
    }
}
