using CloudFabric.EAV.Domain.Enums;

namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class FileAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public bool IsDownloadable { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.File;
    }
}