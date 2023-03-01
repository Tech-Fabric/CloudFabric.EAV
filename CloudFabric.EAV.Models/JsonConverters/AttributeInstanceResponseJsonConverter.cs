using System.Text.Json;
using System.Text.Json.Serialization;

namespace CloudFabric.EAV.Models.JsonConverters;

// Created for derived classes serialization.
// By default only properties from base class are serialized,
// but passing the actual type of the object as a parameter of JsonSerializer.Serialize
// method allows to serialize the properties of the derived classes as well.
public class AttributeInstanceResponseJsonConverter<T> : JsonConverter<T>
{
    public override bool CanConvert(Type type)
    {
        return typeof(T).IsAssignableFrom(type);
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
    }
}
