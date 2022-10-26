﻿using AutoMapper;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.ViewModels;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class LocalizedStringMappingProfile : Profile
{
    public LocalizedStringMappingProfile()
    {
        CreateMap<LocalizedString, LocalizedStringCreateRequest>();
        CreateMap<LocalizedString, LocalizedStringViewModel>();

        CreateMap<LocalizedStringCreateRequest, LocalizedString>();

        CreateMap<LocalizedStringViewModel, LocalizedStringCreateRequest>();
        CreateMap<LocalizedStringViewModel, LocalizedString>();
    }
}
