using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Models.Base;

public class SearchableLocalizedString : LocalizedString
{
#pragma warning disable CA1720 // Identifier contains type name

    [ProjectionDocumentProperty(IsSearchable = true)]
    public override string String { get; set; }

#pragma warning restore CA1720
}
