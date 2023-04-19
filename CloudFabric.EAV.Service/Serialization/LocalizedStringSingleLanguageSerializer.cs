using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Models.ViewModels;

namespace CloudFabric.EAV.Service.Serialization;

public class LocalizedStringSingleLanguageSerializer : JsonConverter<List<LocalizedStringViewModel>>
{
    private readonly CultureInfo _languageCultureInfo;

    private readonly CultureInfo _fallbackLanguageCultureInfo;

    public LocalizedStringSingleLanguageSerializer(
        string language,
        string fallbackLanguage = "en-US"
    )
    {
        _languageCultureInfo = CultureInfo.GetCultureInfo(language);

        _fallbackLanguageCultureInfo = CultureInfo.GetCultureInfo(fallbackLanguage);
    }

    public override List<LocalizedStringViewModel>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, List<LocalizedStringViewModel> value, JsonSerializerOptions options)
    {
        var languageString = value.FirstOrDefault(
                                 v => v.CultureInfoId == _languageCultureInfo.LCID
                             ) ??
                             value.FirstOrDefault(
                                 v => v.CultureInfoId == _fallbackLanguageCultureInfo.LCID
                             );

        writer.WriteStringValue(languageString?.String);
    }
}
