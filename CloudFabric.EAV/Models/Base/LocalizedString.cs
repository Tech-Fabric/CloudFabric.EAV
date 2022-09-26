using System.Globalization;

namespace CloudFabric.EAV.Data.Models.Base;

public class LocalizedString
{
    public int CultureInfoId { get; set; }

#pragma warning disable CA1720 // Identifier contains type name
    public string String { get; set; }
#pragma warning restore CA1720 // Identifier contains type name

    public static LocalizedString Russian(string stringRu)
    {
        return new LocalizedString
        {
            String = stringRu,
            CultureInfoId = CultureInfo.GetCultureInfo("RU-ru").LCID
        };
    }

    public static LocalizedString English(string stringEn)
    {
        return new LocalizedString
        {
            String = stringEn,
            CultureInfoId = CultureInfo.GetCultureInfo("EN-us").LCID
        };
    }
}