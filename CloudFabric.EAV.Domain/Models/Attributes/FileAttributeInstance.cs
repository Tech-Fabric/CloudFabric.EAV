namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class FileAttributeInstance : AttributeInstance
    {
        public FileAttributeValue Value { get; set; }

        public override object? GetValue()
        {
            return Value;
        }
    }

    public class FileAttributeValue
    {
        public string Url { get; set; }

        public string Filename { get; set; }

        public long? Filesize { get; set; }
    }
}
