using AutoMapper;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Projections.EntityConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class EntityConfigurationMappingProfile: Profile
{
    public EntityConfigurationMappingProfile()
    {
        CreateMap<EntityConfigurationCreateRequest, EntityConfiguration>();
        CreateMap<EntityConfigurationUpdateRequest, EntityConfiguration>();

        CreateMap<EntityConfiguration, EntityConfigurationViewModel>();

        CreateMap<EntityConfigurationProjectionDocument, EntityConfigurationViewModel>();
    }
}
