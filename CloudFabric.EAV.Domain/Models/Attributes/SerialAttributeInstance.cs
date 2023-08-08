namespace CloudFabric.EAV.Domain.Models.Attributes;

public class SerialAttributeInstance : AttributeInstance
{
    /// <summary>
    /// Nullable because it's auto-generated
    /// and should be stored before auto-generated value can be calculated.
    /// /// </summary>
    public long? Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
