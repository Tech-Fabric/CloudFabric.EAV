using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class AttributeConfigurationProfile : Profile
{
    public AttributeConfigurationProfile()
    {
        CreateMap<AttributeConfigurationCreateUpdateRequest, AttributeConfiguration>().IncludeAllDerived();

        CreateMap<EntityAttributeConfigurationCreateUpdateReferenceRequest, EntityConfigurationAttributeReference>();

        CreateMap<ArrayAttributeConfigurationCreateUpdateRequest, ArrayAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new ArrayAttributeConfiguration(
                Guid.NewGuid(),
                o.MachineName,
                ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                o.ItemsType,
                Guid.Empty, //o.ItemsAttributeConfiguration,
                ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                o.IsRequired,
                o.TenantId
            ));
        //CreateMap<FileFieldConfigurationCreateUpdateRequest, FileFieldConfiguration>();
        CreateMap<HtmlTextAttributeConfigurationCreateUpdateRequest, HtmlTextAttributeConfiguration>();
        CreateMap<ImageAttributeConfigurationCreateUpdateRequest, ImageAttributeConfiguration>()
            .ConstructUsing((o, ctx) => new ImageAttributeConfiguration(
                Guid.NewGuid(),
                o.MachineName,
                ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                ctx.Mapper.Map<ImageAttributeValue>(o.DefaultValue),
                ctx.Mapper.Map<List<ImageThumbnailDefinition>>(o.ThumbnailsConfiguration),
                ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                o.IsRequired,
                o.TenantId
            ));
        CreateMap<ImageThumbnailDefinitionCreateUpdateRequest, ImageThumbnailDefinition>();
        CreateMap<LocalizedTextAttributeConfigurationCreateUpdateRequest, LocalizedTextAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new LocalizedTextAttributeConfiguration(
                Guid.NewGuid(),
                o.MachineName,
                ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                ctx.Mapper.Map<LocalizedString>(o.DefaultValue),
                ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                o.IsRequired,
                o.TenantId
            ));
        CreateMap<EntityReferenceAttributeConfigurationCreateUpdateRequest, EntityReferenceAttributeConfiguration>();
        CreateMap<NumberAttributeConfigurationCreateUpdateRequest, NumberAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new NumberAttributeConfiguration(
                Guid.NewGuid(),
                o.MachineName,
                ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                o.DefaultValue,
                ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                o.IsRequired,
                o.MinimumValue,
                o.MaximumValue,
                o.TenantId
            ));
        CreateMap<TextAttributeConfigurationCreateUpdateRequest, TextAttributeConfiguration>()
            .ConvertUsing((src, dst, ctx) =>
            {
                var r = new TextAttributeConfiguration(
                    Guid.NewGuid(),
                    src.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(src.Name),
                    src.DefaultValue,
                    src.MaxLength,
                    src.IsSearchable,
                    ctx.Mapper.Map<List<LocalizedString>>(src.Description),
                    src.IsRequired,
                    src.TenantId
                );
                return r;
            });
        CreateMap<ValueFromListConfigurationCreateUpdateRequest, ValueFromListAttributeConfiguration>()
            .ConvertUsing((src, _, ctx) =>
            {
                var r = new ValueFromListAttributeConfiguration(
                    Guid.NewGuid(),
                    src.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(src.Name),
                    src.ValueFromListAttributeType,
                    src.ValuesList,
                    src.AttributeMachineNameToAffect,
                    ctx.Mapper.Map<List<LocalizedString>>(src.Description),
                    src.IsRequired,
                    src.TenantId
                );
                return r;
            });

        CreateMap<EntityConfigurationAttributeReference, EntityConfigurationAttributeReferenceViewModel>();

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
        CreateMap(ValueFromListAttributeConfiguration, ValueFromListAttributeConfigurationViewModel > ();

        #region Projections

        CreateMap<AttributeConfigurationProjectionDocument, AttributeConfigurationListItemViewModel>();

        #endregion
    }
}