using CloudFabric.Projections.Attributes;

namespace CloudFabric.EAV.Domain.Projections
{
    public class LocalizedStringProjectionModel
    {
        [ProjectionDocumentProperty(IsFilterable = true)]
        public int CultureInfoId { get; set; }

#pragma warning disable CA1720 // Identifier contains type name
        [ProjectionDocumentProperty(IsSearchable = true)]
        public string String { get; set; }
#pragma warning restore CA1720 // Identifier contains type name
    }
}