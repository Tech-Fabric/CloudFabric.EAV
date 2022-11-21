using System;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Json.Utilities;

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Models.ViewModels.Attributes;

[JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfigurationViewModel>))]
public abstract class AttributeConfigurationViewModel
{
    public Guid Id { get; set; }
    public bool IsRequired { get; set; }

    public List<LocalizedStringViewModel> Name { get; set; }

    public List<LocalizedStringViewModel> Description { get; set; }

    public string MachineName { get; set; }

    public EavAttributeType ValueType { get; set; }
}