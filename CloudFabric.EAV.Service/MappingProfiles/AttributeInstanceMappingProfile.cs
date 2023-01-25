using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class AttributeInstanceProfile : Profile
{
    public AttributeInstanceProfile()
    {
        CreateMap<AttributeInstanceCreateUpdateRequest, AttributeInstance>().IncludeAllDerived();
        CreateMap<ArrayAttributeInstanceCreateUpdateRequest, ArrayAttributeInstance>();
        //CreateMap<FileFieldInstanceCreateUpdateRequest, FileFieldInstance>();
        CreateMap<HtmlTextAttributeInstanceCreateUpdateRequest, HtmlTextAttributeInstance>();
        CreateMap<ImageAttributeInstanceCreateUpdateRequest, ImageAttributeInstance>();
        CreateMap<ImageAttributeValueCreateUpdateRequest, ImageAttributeValue>();
        CreateMap<ImageThumbnailDefinitionCreateUpdateRequest, ImageThumbnailDefinition>();
        CreateMap<LocalizedTextAttributeInstanceCreateUpdateRequest, LocalizedTextAttributeInstance>();
        CreateMap<EntityReferenceAttributeInstanceCreateUpdateRequest, EntityReferenceAttributeInstance>();
        CreateMap<NumberAttributeInstanceCreateUpdateRequest, NumberAttributeInstance>();
        CreateMap<TextAttributeInstanceCreateUpdateRequest, TextAttributeInstance>();
        CreateMap<ValueFromListAttributeInstanceCreateUpdateRequest, ValueFromListAttributeInstance>();
        CreateMap<DateRangeAttributeInstanceCreateUpdateRequest, DateRangeAttributeInstance>();
        CreateMap<BooleanAttributeInstanceCreateUpdateRequest, BooleanAttributeInstance>();
        CreateMap<FileAttributeValueCreateUpdateRequest, FileAttributeValue>();
        CreateMap<FileAttributeInstanceCreateUpdateRequest, FileAttributeInstance>();

        CreateMap<AttributeInstance, AttributeInstanceViewModel>().IncludeAllDerived();
        CreateMap<ArrayAttributeInstance, ArrayAttributeInstanceViewModel>();
        //CreateMap<FileFieldInstance, FileFieldInstanceViewModel>();
        CreateMap<HtmlTextAttributeInstance, HtmlTextAttributeInstanceViewModel>();
        CreateMap<ImageAttributeInstance, ImageAttributeInstanceViewModel>();
        CreateMap<ImageAttributeValue, ImageAttributeValueViewModel>();
        CreateMap<ImageThumbnailDefinition, ImageThumbnailDefinitionViewModel>();
        CreateMap<LocalizedTextAttributeInstance, LocalizedTextAttributeInstanceViewModel>();
        CreateMap<EntityReferenceAttributeInstance, EntityReferenceAttributeInstanceViewModel>();
        CreateMap<NumberAttributeInstance, NumberAttributeInstanceViewModel>();
        CreateMap<TextAttributeInstance, TextAttributeInstanceViewModel>();
        CreateMap<ValueFromListAttributeInstance, ValueFromListAttributeInstanceViewModel>();
        CreateMap<DateRangeAttributeInstance, DateRangeAttributeInstanceViewModel>();
        CreateMap<BooleanAttributeInstance, BooleanAttributeInstanceViewModel>();
        CreateMap<FileAttributeInstance, FileAttributeInstanceViewModel>();
        CreateMap<FileAttributeValue, FileAttributeValueViewModel>();
    }
}