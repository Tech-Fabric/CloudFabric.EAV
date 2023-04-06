using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Json.Utilities;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Models.JsonConverters;

public class AttributeRequestJsonConverter<T> : JsonConverter<T>
{
    public override bool CanConvert(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        JsonNode? obj = JsonNode.Parse(ref reader);
        if (obj == null)
        {
            throw new InvalidOperationException();
        }

        if (obj[PolymorphicJsonConverter<T>.TYPE_NAME_JSON_PROPERTY_NAME] != null)
        {
            var newOptions = new JsonSerializerOptions(options);
            newOptions.Converters.Add(new PolymorphicJsonConverter<T>());
            var newJson = obj.ToString();
            return (T)JsonSerializer.Deserialize(newJson, typeToConvert, newOptions)!
                   ?? throw new InvalidOperationException();
        }

        var attributeTypeName = Enum.Parse<EavAttributeType>(obj["valueType"]?.ToString()
                                                             ?? throw new InvalidOperationException()
        );
        Type type = GetRequestTypeFromAttributeType(attributeTypeName);

        var attributeRequestModel = JsonSerializer.Deserialize(obj.ToString(), type, options);
        return (T)attributeRequestModel! ?? throw new InvalidOperationException();
    }

    private static Type GetRequestTypeFromAttributeType(EavAttributeType attributeTypeName)
    {
        switch (attributeTypeName)
        {
            case EavAttributeType.Number:
                return typeof(NumberAttributeConfigurationCreateUpdateRequest);
            case EavAttributeType.LocalizedText:
                return typeof(LocalizedTextAttributeConfigurationCreateUpdateRequest);
            case EavAttributeType.Text:
                return typeof(TextAttributeConfigurationCreateUpdateRequest);
            case EavAttributeType.DateRange:
                return typeof(DateRangeAttributeConfigurationUpdateRequest);
            case EavAttributeType.Boolean:
                return typeof(BooleanAttributeConfigurationCreateUpdateRequest);
            case EavAttributeType.File:
                return typeof(FileAttributeConfigurationCreateUpdateRequest);
            case EavAttributeType.ValueFromList:
                return typeof(ValueFromListAttributeConfigurationCreateUpdateRequest);
            case EavAttributeType.Serial:
                return typeof(SerialAttributeConfigurationCreateRequest);
            case EavAttributeType.Image:
                return typeof(ImageAttributeConfigurationCreateUpdateRequest);
            default:
                throw new InvalidOperationException();
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
