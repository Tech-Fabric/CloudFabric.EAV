using System;
using System.Globalization;

namespace CloudFabric.EAV.Domain.Models.Base;

public class LocalizedString: IEquatable<LocalizedString>
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
    public bool Equals(LocalizedString other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return CultureInfoId == other.CultureInfoId && String == other.String;
    }
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        return obj.GetType() == this.GetType() && Equals((LocalizedString)obj);

    }
    public override int GetHashCode()
    {
        return HashCode.Combine(CultureInfoId, String);
    }
}