using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class EntityConfigurationMappingProfile : Profile
{
    public EntityConfigurationMappingProfile()
    {
        CreateMap<EntityConfigurationCreateRequest, EntityConfiguration>();
        CreateMap<EntityConfigurationCreateRequest, EntityConfigurationCreateRequest>();
        CreateMap<EntityConfigurationUpdateRequest, EntityConfiguration>();

        CreateMap<EntityConfiguration, EntityConfigurationViewModel>();
        CreateMap<EntityConfiguration, EntityConfigurationWithAttributesViewModel>()
            .ForMember(
                c => c.Attributes,
                memberOptions => memberOptions.Ignore()
            );

        CreateMap<EntityConfigurationProjectionDocument, EntityConfigurationViewModel>();
        CreateMap<AttributeConfigurationReference, EntityConfigurationAttributeReferenceViewModel>();
        CreateMap<ProjectionQueryResult<EntityConfigurationProjectionDocument>,
            ProjectionQueryResult<EntityConfigurationViewModel>>();
        CreateMap<QueryResultDocument<EntityConfigurationProjectionDocument>,
            QueryResultDocument<EntityConfigurationViewModel>>();
    }
}
