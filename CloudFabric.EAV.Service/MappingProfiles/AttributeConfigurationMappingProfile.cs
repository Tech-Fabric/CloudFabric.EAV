using System.Text.Encodings.Web;
using AutoMapper;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.AttributeValidationRules;
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
        CreateMap<NumberAttributeConfigurationCreateUpdateRequest, NumberAttributeConfiguration>()
            .ForMember(dest => dest.ValidationRules, opt => opt.ConvertUsing(new NumberValidationRuleRequestConverter(), src => src.Validators));
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

public abstract class ValidationRuleRequestConverter<T> : IValueConverter<T, List<AttributeValidationRule>> where T: AttributeConfigurationValidationRequest
{
    protected abstract void ConvertRules(T source, List<AttributeValidationRule> rules);
    public List<AttributeValidationRule> Convert(T sourceMember, ResolutionContext context)
    {
        var rules = new List<AttributeValidationRule>();
        if (sourceMember.IsRequired) rules.Add(new RequiredAttributeValidationRule());
        ConvertRules(sourceMember, rules);
        return rules;
    }
}

public class NumberValidationRuleRequestConverter : ValidationRuleRequestConverter<NumberAttributeConfigurationValidationRequest>
{
    protected override void ConvertRules(NumberAttributeConfigurationValidationRequest source, List<AttributeValidationRule> rules)
    {
       if (source.MinimumValue != null) { rules.Add(new MinimumValueValidationRule(source.MinimumValue.Value));} 
       if (source.MaximumValue != null) { rules.Add(new MaximumValueValidationRule(source.MaximumValue.Value));}
    }
}