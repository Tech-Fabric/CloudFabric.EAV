using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Enums;
using CloudFabric.EAV.Models.RequestModels.Attributes;

namespace CloudFabric.EAV.Models.JsonConverters;

public class AttributeInstanceRequestJsonConverter<T> : JsonConverter<T>
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

        var attributeTypeName =
            Enum.Parse<EavAttributeType>(obj["valueType"]?.ToString() ?? throw new InvalidOperationException());
        Type type = GetRequestTypeFromAttributeType(attributeTypeName);

        var updatedOptions = new JsonSerializerOptions(options);
        updatedOptions.PropertyNameCaseInsensitive = true;
        updatedOptions.Converters.Add(new JsonStringEnumConverter(updatedOptions.PropertyNamingPolicy));

        var attributeRequestModel = JsonSerializer.Deserialize(obj.ToString(), type, updatedOptions);
        return (T)attributeRequestModel! ?? throw new InvalidOperationException();
    }

    private static Type GetRequestTypeFromAttributeType(EavAttributeType attributeTypeName)
    {
        switch (attributeTypeName)
        {
            case EavAttributeType.Number:
                return typeof(NumberAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.LocalizedText:
                return typeof(LocalizedTextAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.Text:
                return typeof(TextAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.DateRange:
                return typeof(DateRangeAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.Boolean:
                return typeof(BooleanAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.File:
                return typeof(FileAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.ValueFromList:
                return typeof(ValueFromListAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.Serial:
                return typeof(SerialAttributeInstanceCreateUpdateRequest);
            case EavAttributeType.Image:
                return typeof(ImageAttributeInstanceCreateUpdateRequest);
            default:
                throw new InvalidOperationException();
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
