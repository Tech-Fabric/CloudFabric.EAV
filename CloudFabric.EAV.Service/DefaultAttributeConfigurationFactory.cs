using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Enums;

namespace CloudFabric.EAV.Service;

public static class DefaultAttributeConfigurationFactory
{
    public static AttributeConfiguration? GetDefaultConfiguration(EavAttributeType type, string machineName, Guid? tenantId)
    {
        return type switch
        {
            EavAttributeType.Money => new MoneyAttributeConfiguration(machineName, tenantId),
            EavAttributeType.Boolean => new BooleanAttributeConfiguration(machineName, tenantId),
            EavAttributeType.DateRange => new DateRangeAttributeConfiguration(machineName, tenantId),
            EavAttributeType.File => new FileAttributeConfiguration(machineName, tenantId),
            EavAttributeType.Image => new ImageAttributeConfiguration(machineName, tenantId),
            EavAttributeType.LocalizedText => new LocalizedTextAttributeConfiguration(machineName, tenantId),
            EavAttributeType.Number => new NumberAttributeConfiguration(machineName, tenantId),
            EavAttributeType.Text => new TextAttributeConfiguration(machineName, tenantId),
            _ => null
        };
    }
}
