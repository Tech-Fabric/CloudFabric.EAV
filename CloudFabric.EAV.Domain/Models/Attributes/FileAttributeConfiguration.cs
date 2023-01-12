using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EAV.Domain.Options;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class FileAttributeConfiguration : AttributeConfiguration
    {
        public bool IsDownloadable { get; set; }

        public override EavAttributeType ValueType { get; } = EavAttributeType.File;

        public FileAttributeConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }

        public FileAttributeConfiguration(
            Guid id,
            string machineName,
            List<LocalizedString> name,
            bool isDownloadable,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? tenantId = null
        ) : base(id, machineName, name, EavAttributeType.File, description, isRequired, tenantId)
        {
            Apply(new FileAttributeConfigurationUpdated(id, isDownloadable));
        }

        public override void UpdateAttribute(AttributeConfiguration updatedAttribute)
        {
            var updated = updatedAttribute as FileAttributeConfiguration;

            if (updated == null)
            {
                throw new ArgumentException("Invalid attribute type");
            }

            base.UpdateAttribute(updatedAttribute);

            if (IsDownloadable != updated.IsDownloadable)
            {
                Apply(new FileAttributeConfigurationUpdated(Id, updated.IsDownloadable));
            }
        }

        public override List<string> Validate(AttributeInstance? instance, AttributeValidationRuleOptions? validationRules)
        {
            var errors = base.Validate(instance, validationRules);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not FileAttributeInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: File");
                return errors;
            }

            var attributeInstance = instance as FileAttributeInstance;

            // validate file extension and size
            var extension = attributeInstance!.Value.Filename.Substring(
                attributeInstance.Value.Filename.LastIndexOf('.')
            );

            var availableExtensions = validationRules?.AllowedFileExtensions?.Split('|', ',', ';');

            if (string.IsNullOrEmpty(extension) || (availableExtensions != null && !availableExtensions.Contains(extension)))
            {
                errors.Add("Unsupported file extension");
            }

            if (!attributeInstance.Value.Filesize.HasValue
                || (validationRules?.MaxFileSizeInBytes != null && attributeInstance.Value.Filesize > validationRules.MaxFileSizeInBytes))
            {
                errors.Add($"File size cannot be greated than {validationRules!.MaxFileSizeInBytes}");
            }

            return errors;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as FileAttributeConfiguration);
        }

        private bool Equals(FileAttributeConfiguration other)
        {
            return base.Equals(other)
                   && IsDownloadable == other.IsDownloadable
                   && ValueType == other.ValueType;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(IsDownloadable, (int)ValueType);
        }

        #region EventHandlers

        public void On(FileAttributeConfigurationUpdated @event)
        {
            Id = @event.AggregateId;
            IsDownloadable = @event.IsDownloadable;
        }

        #endregion
    }
}