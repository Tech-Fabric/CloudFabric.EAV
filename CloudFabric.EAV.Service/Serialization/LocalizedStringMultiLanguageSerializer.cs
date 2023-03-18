using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Service.Serialization;

public class LocalizedStringMultiLanguageSerializer: JsonConverter<List<LocalizedString>>
{
    public override List<LocalizedString>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, List<LocalizedString> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        foreach (var localizedString in value)
        {
            var cultureInfo = new CultureInfo(localizedString.CultureInfoId);
            writer.WritePropertyName(cultureInfo.Name);
            writer.WriteStringValue(localizedString.String);
        }

        writer.WriteEndObject();
    }
}
