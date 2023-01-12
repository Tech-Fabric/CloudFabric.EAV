namespace CloudFabric.EAV.Domain.Options
{
    public class AttributeValidationRuleOptions
    {
        #region File rules

        public long? MaxFileSizeInBytes { get; set; }
        public string? AllowedFileExtensions { get; set; }

        #endregion
    }
}