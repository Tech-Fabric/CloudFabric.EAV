using System.Text.Json.Serialization;

using CloudFabric.EAV.Models.JsonConverters;

namespace CloudFabric.EAV.Models.ViewModels.Attributes;

[JsonConverter(typeof(AttributeInstanceResponseJsonConverter<AttributeInstanceViewModel>))]
public abstract class AttributeInstanceViewModel
{
    public string ConfigurationAttributeMachineName { get; set; }

    public abstract object? GetValue();
}
