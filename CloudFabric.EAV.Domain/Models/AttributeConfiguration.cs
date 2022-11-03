using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Json.Utilities;

namespace CloudFabric.EAV.Domain.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeConfiguration>))]
    public abstract class AttributeConfiguration
    {
        public bool IsRequired { get; set; }
        public List<LocalizedString> Name { get; protected set; }

        public List<LocalizedString> Description { get; protected set; }

        public string MachineName { get; protected set; }

        public abstract EavAttributeType ValueType { get; }

        public virtual List<string> Validate(AttributeInstance instance)
        {
            if (IsRequired && instance == null)
            {
                return new List<string>() { "Attribute is Required" };
            }

            return new List<string>();
        }

        public override bool Equals(object obj){
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj.GetType() == GetType() && Equals(obj as AttributeConfiguration);
        }

        private bool Equals(AttributeConfiguration obj)
        {
            return obj != null 
                   && (Name.SequenceEqual(obj.Name) 
                       && Description.SequenceEqual(obj.Description) 
                       && IsRequired == obj.IsRequired 
                       && MachineName == obj.MachineName
                       && ValueType == obj.ValueType);
        }
    }
}