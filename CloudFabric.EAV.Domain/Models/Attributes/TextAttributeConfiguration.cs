using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class TextAttributeConfiguration : AttributeConfiguration
    {
        public string DefaultValue { get; set; }

        public bool IsSearchable { get; set; }
        
        public int? MaxLength { get; set; }
        
        public override EavAttributeType ValueType { get; } = EavAttributeType.Text;
        
        public TextAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
            
        }
        
        public TextAttributeConfiguration(
            Guid id, 
            string machineName, 
            List<LocalizedString> name,
            string defaultValue,
            int? maxLength,
            bool isSearchable,
            List<LocalizedString> description = null, 
            bool isRequired = false,
            Guid? tenantId = null
        ) : base(id, machineName, name, EavAttributeType.Text, description, isRequired, tenantId) 
        {
            Apply(new TextAttributeConfigurationUpdated(defaultValue, maxLength, isSearchable));
        }
        
        public override List<string> Validate(AttributeInstance? instance)
        {
            var errors = base.Validate(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not TextAttributeInstance textInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Text)");
                return errors;
            }

            if (IsRequired && string.IsNullOrWhiteSpace(textInstance.Value))
            {
                errors.Add("Cannot validate attribute. Expected attribute type: Text)");
                return errors;
            }

            if (MaxLength.HasValue && textInstance.Value.Length > MaxLength)
            {
                errors.Add($"Text length can't be greater than {MaxLength}");
            }
            
            return errors;
        }
        
        public override bool Equals(object obj)
        {
            return this.Equals(obj as TextAttributeConfiguration);
        }

        private bool Equals(TextAttributeConfiguration other)
        {
            return base.Equals(other)
                   && DefaultValue.Equals(other.DefaultValue)
                   && Nullable.Equals(MaxLength, other.MaxLength)
                   && IsSearchable == other.IsSearchable
                   && ValueType == other.ValueType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DefaultValue, MaxLength, IsSearchable, (int)ValueType);
        }
        
        #region EventHandlers

        public void On(TextAttributeConfigurationUpdated @event)
        {
            DefaultValue = @event.DefaultValue;
            MaxLength = @event.MaxLength;
            IsSearchable = @event.IsSearchable;
        }

        #endregion
    }
}