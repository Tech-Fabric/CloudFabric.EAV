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
        writer.WriteStartObject();

        writer.WriteString("id", value.Id);
        writer.WriteString("entityConfigurationId", value.EntityConfigurationId);
        writer.WriteString("tenantId", value.TenantId.ToString());

        writer.WritePropertyName("categoryPaths");
        writer.WriteStartArray();
        foreach (var categoryPath in value.CategoryPaths)
        {
            var categoryPathSerialized = JsonSerializer.Serialize(categoryPath, options);
            writer.WriteRawValue(categoryPathSerialized);
        }
        writer.WriteEndArray();

        foreach (var attribute in value.Attributes)
        {
            writer.WritePropertyName(attribute.ConfigurationAttributeMachineName);

            var attributeValue = attribute.GetValue();

            if (attributeValue == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                var valueSerialized = JsonSerializer.Serialize(attributeValue, options);
                writer.WriteRawValue(valueSerialized);
            }
        }

        writer.WriteString("partitionKey", value.PartitionKey);
        writer.WriteNumber("version", value.Version);

        writer.WriteEndObject();
    }
}
