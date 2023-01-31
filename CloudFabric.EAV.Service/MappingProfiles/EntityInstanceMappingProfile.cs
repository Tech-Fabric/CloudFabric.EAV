using AutoMapper;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels.EAV;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class EntityInstanceProfile : Profile
{
    public EntityInstanceProfile()
    {
        CreateMap<EntityInstanceCreateRequest, EntityInstance>().IncludeAllDerived();
        CreateMap<EntityInstanceUpdateRequest, EntityInstance>();

        CreateMap<EntityInstance, EntityInstanceViewModel>();
        CreateMap<EntityInstanceViewModel, EntityInstance>();
        CreateMap<EntityInstanceViewModel, EntityTreeInstanceViewModel>().ForMember(o => o.Children, opt => opt.MapFrom(_ => new List<EntityTreeInstanceViewModel>()));
    }
}
