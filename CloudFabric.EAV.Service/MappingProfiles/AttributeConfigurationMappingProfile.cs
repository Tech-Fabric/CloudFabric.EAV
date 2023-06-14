using AutoMapper;

using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Projections.AttributeConfigurationProjection;
using CloudFabric.EAV.Models.RequestModels;
using CloudFabric.EAV.Models.RequestModels.Attributes;
using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;
using CloudFabric.Projections;
using CloudFabric.Projections.Queries;

namespace CloudFabric.EAV.Service.MappingProfiles;

public class AttributeConfigurationProfile : Profile
{
    public AttributeConfigurationProfile()
    {
        CreateMap<AttributeConfigurationCreateUpdateRequest, AttributeConfiguration>().IncludeAllDerived();

        CreateMap<EntityAttributeConfigurationCreateUpdateReferenceRequest,
            EntityConfigurationAttributeReference>();

        CreateMap<ArrayAttributeConfigurationCreateUpdateRequest, ArrayAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new ArrayAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    o.ItemsType,
                    Guid.Empty,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );
        //CreateMap<FileFieldConfigurationCreateUpdateRequest, FileFieldConfiguration>();
        CreateMap<HtmlTextAttributeConfigurationCreateUpdateRequest, HtmlTextAttributeConfiguration>();
        CreateMap<ImageAttributeConfigurationCreateUpdateRequest, ImageAttributeConfiguration>()
            .ConstructUsing((o, ctx) => new ImageAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    ctx.Mapper.Map<List<ImageThumbnailDefinition>>(o.ThumbnailsConfiguration),
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );
        CreateMap<ImageThumbnailDefinitionCreateUpdateRequest, ImageThumbnailDefinition>();

        CreateMap<LocalizedTextAttributeConfigurationCreateUpdateRequest, LocalizedTextAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new LocalizedTextAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    ctx.Mapper.Map<LocalizedString>(o.DefaultValue),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );
        CreateMap<EntityReferenceAttributeConfigurationCreateUpdateRequest,
            EntityReferenceAttributeConfiguration>();
        CreateMap<NumberAttributeConfigurationCreateUpdateRequest, NumberAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new NumberAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    o.NumberType,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.DefaultValue,
                    o.MinimumValue,
                    o.MaximumValue,
                    o.TenantId,
                    o.Metadata
                )
            );
        CreateMap<TextAttributeConfigurationCreateUpdateRequest, TextAttributeConfiguration>()
            .ConvertUsing((src, dst, ctx) =>
                {
                    var r = new TextAttributeConfiguration(
                        Guid.NewGuid(),
                        src.MachineName,
                        ctx.Mapper.Map<List<LocalizedString>>(src.Name),
                        src.MaxLength,
                        src.IsSearchable,
                        ctx.Mapper.Map<List<LocalizedString>>(src.Description),
                        src.DefaultValue,
                        src.IsRequired,
                        src.TenantId,
                        src.Metadata
                    );
                    return r;
                }
            );
        CreateMap<DateRangeAttributeConfigurationUpdateRequest, DateRangeAttributeConfiguration>()
            .ConvertUsing((src, _, ctx) => new DateRangeAttributeConfiguration(
                    Guid.NewGuid(),
                    src.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(src.Name),
                    src.DateRangeAttributeType,
                    ctx.Mapper.Map<List<LocalizedString>>(src.Description),
                    src.IsRequired,
                    src.TenantId,
                    src.Metadata
                )
            );
        CreateMap<ValueFromListAttributeConfigurationCreateUpdateRequest, ValueFromListAttributeConfiguration>()
            .ConvertUsing((src, _, ctx) =>
                {
                    var r = new ValueFromListAttributeConfiguration(
                        Guid.NewGuid(),
                        src.MachineName,
                        ctx.Mapper.Map<List<LocalizedString>>(src.Name),
                        ctx.Mapper.Map<List<ValueFromListOptionConfiguration>>(src.ValuesList),
                        ctx.Mapper.Map<List<LocalizedString>>(src.Description),
                        src.IsRequired,
                        src.TenantId,
                        src.Metadata
                    );
                    return r;
                }
            );

        CreateMap<BooleanAttributeConfigurationCreateUpdateRequest, BooleanAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new BooleanAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    o.TrueDisplayValue,
                    o.FalseDisplayValue,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );

        CreateMap<FileAttributeConfigurationCreateUpdateRequest, FileAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new FileAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    o.IsDownloadable,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );

        CreateMap<SerialAttributeConfigurationCreateRequest, SerialAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new SerialAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    o.StartingNumber,
                    o.Increment,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );

        // Use 0 as a StartingNumber default value for SerialAttributeConfiguration constructor.
        // Value doesn't participate in attribute update.
        CreateMap<SerialAttributeConfigurationUpdateRequest, SerialAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new SerialAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    0,
                    o.Increment,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );

        CreateMap<CurrencyRequestModel, Currency>();
        CreateMap<MoneyAttributeConfigurationCreateUpdateRequest, MoneyAttributeConfiguration>()
            .ConvertUsing((o, dst, ctx) => new MoneyAttributeConfiguration(
                    Guid.NewGuid(),
                    o.MachineName,
                    ctx.Mapper.Map<List<LocalizedString>>(o.Name),
                    o.DefaultCurrencyId,
                    ctx.Mapper.Map<List<Currency>>(o.Currencies),
                    ctx.Mapper.Map<List<LocalizedString>>(o.Description),
                    o.IsRequired,
                    o.TenantId,
                    o.Metadata
                )
            );

        CreateMap<ValueFromListOptionCreateUpdateRequest, ValueFromListOptionConfiguration>();

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
        CreateMap<DateRangeAttributeConfiguration, DateRangeAttributeConfigurationViewModel>();
        CreateMap<BooleanAttributeConfiguration, BooleanAttributeConfigurationViewModel>();
        CreateMap<FileAttributeConfiguration, FileAttributeConfigurationViewModel>();
        CreateMap<SerialAttributeConfiguration, SerialAttributeConfigurationViewModel>();
        CreateMap<ValueFromListAttributeConfiguration, ValueFromListAttributeConfigurationViewModel>();
        CreateMap<ValueFromListOptionConfiguration, ValueFromListOptionViewModel>();
        CreateMap<Currency, CurrencyViewModel>();
        CreateMap<MoneyAttributeConfiguration, MoneyAttributeConfigurationViewModel>();
        #region Projections

        CreateMap<AttributeConfigurationProjectionDocument, AttributeConfigurationListItemViewModel>();
        CreateMap<ProjectionQueryResult<AttributeConfigurationProjectionDocument>,
            ProjectionQueryResult<AttributeConfigurationListItemViewModel>>();
        CreateMap<QueryResultDocument<AttributeConfigurationProjectionDocument>,
            QueryResultDocument<AttributeConfigurationListItemViewModel>>();

        #endregion
    }
}
