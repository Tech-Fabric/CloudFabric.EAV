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
        CreateMap<EntityInstanceViewModel, EntityInstance>();


        CreateMap<CategoryTreeCreateRequest, CategoryTree>();
        CreateMap<CategoryTree, HierarchyViewModel>();

        CreateMap<CategoryInstanceCreateRequest, Category>();
        CreateMap<Category, CategoryViewModel>();
        CreateMap<Category, EntityInstanceViewModel>();
        CreateMap<CategoryViewModel, EntityTreeInstanceViewModel>().ForMember(o => o.Children,
            opt => opt.MapFrom(_ => new List<EntityTreeInstanceViewModel>())
        );

        CreateMap<EntityInstance, Category>();
        CreateMap<Category, EntityInstance>();

        CreateMap<EntityInstanceViewModel, CategoryViewModel>();

        CreateMap<CategoryPath, CategoryPathViewModel>();
        CreateMap<CategoryPathViewModel, CategoryPath>();
    }
}
