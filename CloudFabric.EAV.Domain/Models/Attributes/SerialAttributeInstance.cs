namespace CloudFabric.EAV.Domain.Models.Attributes;

public class SerialAttributeInstance : AttributeInstance
{
    /// <summary>
    /// Nullable type is used to separate logic of create and update flows
    /// and to avoid splitting CreateUpdate logic into two separate ones
    /// which leads to JsonConverter issues.
    /// 
    /// During create entity instance this value has no affect, as
    /// it is assigned with external value of entity configuration.
    /// 
    /// During update entity instance, after validation and
    /// required actions with external entity configuration values,
    /// this value is assigned as a new one
    /// </summary>
    public long? Value { get; set; }

    public override object? GetValue()
    {
        return Value;
    }
}
