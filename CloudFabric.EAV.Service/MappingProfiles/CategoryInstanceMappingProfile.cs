using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Models.LocalEventSourcingPackages.RequestModels;
using CloudFabric.EAV.Models.LocalEventSourcingPackages.ViewModels;

namespace CloudFabric.EAV.Service.MappingProfiles
{
    public class CategoryInstanceMappingProfile: Profile
    {
        public CategoryInstanceMappingProfile()
        {
            CreateMap<CategoryInstanceCreateRequest, CategoryInstance>().IncludeAllDerived();
            CreateMap<CategoryInstance, CategoryInstanceViewModel>().IncludeAllDerived();
        }

    }
}