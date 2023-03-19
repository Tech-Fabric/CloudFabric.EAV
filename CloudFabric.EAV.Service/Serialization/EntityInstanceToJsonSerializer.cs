using System.Text.Json;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Domain.Models;

namespace CloudFabric.EAV.Service.Serialization;

public class EntityInstanceToJsonSerializer: JsonConverter<EntityInstance>
{
    public override EntityInstance? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, EntityInstance value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
