using System.Globalization;

using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Models.Base;

public class LocalizedString : IEquatable<LocalizedString>
{
    [ProjectionDocumentProperty(IsFilterable = true)]
    public int CultureInfoId { get; set; }

#pragma warning disable CA1720 // Identifier contains type name
    [ProjectionDocumentProperty(IsFilterable = true)] public virtual string String { get; set; }
#pragma warning restore CA1720 // Identifier contains type name
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

    public static LocalizedString Russian(string stringRu)
    {
        return new LocalizedString { String = stringRu, CultureInfoId = CultureInfo.GetCultureInfo("ru-RU").LCID };
    }

    public static LocalizedString English(string stringEn)
    {
        return new LocalizedString { String = stringEn, CultureInfoId = CultureInfo.GetCultureInfo("en-US").LCID };
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

        return obj.GetType() == GetType() && Equals((LocalizedString)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(CultureInfoId, String);
    }
}
