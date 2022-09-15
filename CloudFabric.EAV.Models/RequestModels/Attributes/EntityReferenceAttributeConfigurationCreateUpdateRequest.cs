using System;
using Nastolkino.Data.Enums;
using Nastolkino.Json.Utilities;

namespace Nastolkino.Service.Models.RequestModels.EAV.Attributes
{
    public class EntityReferenceAttributeConfigurationCreateUpdateRequest : AttributeConfigurationCreateUpdateRequest
    {
        public override EavAttributeType ValueType { get; } = EavAttributeType.EntityReference;
        
        public Guid ReferenceEntityConfiguration { get; set; }
        
        public Guid DefaultValue { get; set; }
    }
}