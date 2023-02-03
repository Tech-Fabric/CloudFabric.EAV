using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Service.Factories
{
    public static class AttributeInstanceFactory
    {
        public static AttributeInstance GetDefaultInstanceValue(AttributeConfiguration attributeConfiguration)
        {
            switch (attributeConfiguration.ValueType)
            {
                case EavAttributeType.Text:
                    return new TextAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = ((TextAttributeConfiguration)attributeConfiguration).DefaultValue
                    };
                case EavAttributeType.Number:
                    return new NumberAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = ((NumberAttributeConfiguration)attributeConfiguration).DefaultValue
                    };
                case EavAttributeType.Boolean:
                    return new BooleanAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = false
                    };
                case EavAttributeType.ValueFromList:
                    return new ValueFromListAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = ((ValueFromListAttributeConfiguration)attributeConfiguration).ValuesList.First().MachineName
                    };
                case EavAttributeType.LocalizedText:
                    return new LocalizedTextAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = new List<LocalizedString>
                        {
                            ((LocalizedTextAttributeConfiguration)attributeConfiguration).DefaultValue
                        }
                    };
                case EavAttributeType.DateRange:
                    return new DateRangeAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = new DateRangeAttributeInstanceValue
                        {
                            From = DateTime.UtcNow,
                            To = null
                        }
                    };
                case EavAttributeType.Image:
                    return new ImageAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = null
                    };
                case EavAttributeType.File:
                    return new FileAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = null
                    };
                case EavAttributeType.Serial:
                    return new SerialAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Value = 0
                    };
                case EavAttributeType.Array:
                    return new ArrayAttributeInstance
                    {
                        ConfigurationAttributeMachineName = attributeConfiguration.MachineName,
                        Items = null
                    };
                default:
                    throw new NotSupportedException($"Instance creation is not supported for {attributeConfiguration.ValueType} type");
            }
        }
    }
}