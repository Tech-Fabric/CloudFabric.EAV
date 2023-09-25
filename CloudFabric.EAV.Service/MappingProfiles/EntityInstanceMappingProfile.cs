using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class EntityInstanceProfile : Profile
{
    public EntityInstanceProfile()
    {
        CreateMap<EntityInstanceCreateRequest, EntityInstance>().IncludeAllDerived();
        CreateMap<EntityInstanceUpdateRequest, EntityInstance>();

        CreateMap<EntityInstance, EntityInstanceViewModel>();
        CreateMap<EntityInstance, EntityTreeInstanceViewModel>();
        CreateMap<EntityInstanceViewModel, EntityInstance>();
        CreateMap<EntityInstanceViewModel, EntityTreeInstanceViewModel>();


        CreateMap<CategoryTreeCreateRequest, CategoryTree>();
        CreateMap<CategoryTree, CategoryTreeViewModel>();

        CreateMap<CategoryPath, CategoryPathViewModel>();
        CreateMap<CategoryPathViewModel, CategoryPath>();

        CreateMap<CategoryPathCreateUpdateRequest, CategoryPath>();
    }
}
