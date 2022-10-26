using AutoMapper;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class AttributeConfigurationProfile : Profile
{
    public AttributeConfigurationProfile()
    {
        CreateMap<AttributeConfigurationCreateUpdateRequest, AttributeConfiguration>().IncludeAllDerived();
        CreateMap<ArrayAttributeConfigurationCreateUpdateRequest, ArrayAttributeConfiguration>();
        //CreateMap<FileFieldConfigurationCreateUpdateRequest, FileFieldConfiguration>();
        CreateMap<HtmlTextAttributeConfigurationCreateUpdateRequest, HtmlTextAttributeConfiguration>();
        CreateMap<ImageAttributeConfigurationCreateUpdateRequest, ImageAttributeConfiguration>();
        CreateMap<ImageThumbnailDefinitionCreateUpdateRequest, ImageThumbnailDefinition>();
        CreateMap<LocalizedTextAttributeConfigurationCreateUpdateRequest, LocalizedTextAttributeConfiguration>();
        CreateMap<EntityReferenceAttributeConfigurationCreateUpdateRequest, EntityReferenceAttributeConfiguration>();
        CreateMap<NumberAttributeConfigurationCreateUpdateRequest, NumberAttributeConfiguration>();
        CreateMap<TextAttributeConfigurationCreateUpdateRequest, TextAttributeConfiguration>();

        CreateMap<AttributeConfiguration, AttributeConfigurationViewModel>().IncludeAllDerived();
        CreateMap<ArrayAttributeConfiguration, ArrayAttributeConfigurationViewModel>();
        //CreateMap<FileFieldConfiguration, FileFieldConfigurationViewModel>();
        CreateMap<HtmlTextAttributeConfiguration, HtmlTextAttributeConfigurationViewModel>();
        CreateMap<ImageAttributeConfiguration, ImageAttributeConfigurationViewModel>();
        CreateMap<ImageThumbnailDefinition, ImageThumbnailDefinitionViewModel>();
        CreateMap<LocalizedTextAttributeConfiguration, LocalizedTextAttributeConfigurationViewModel>();
        CreateMap<EntityReferenceAttributeConfiguration, EntityReferenceAttributeConfigurationViewModel>();
        CreateMap<NumberAttributeConfiguration, NumberAttributeConfigurationViewModel>();
        CreateMap<TextAttributeConfiguration, TextAttributeConfigurationViewModel>();
    }
}
