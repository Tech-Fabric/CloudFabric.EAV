using System;
using System.Collections.Generic;

using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class TextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
        
        public TextAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            List<LocalizedString> description = null, 
            bool isRequired = false
        ) : base(id, machineName, name, EavAttributeType.Text, description, isRequired) {
        }
    }
}