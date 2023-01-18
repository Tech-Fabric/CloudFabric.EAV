using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Events.Configuration.Attributes;
using CloudFabric.EAV.Domain.Models.Base;
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

        public override List<string> ValidateInstance(AttributeInstance? instance)
        {
            var errors = base.ValidateInstance(instance);

            if (instance == null)
            {
                return errors;
            }

            if (instance is not FileAttributeInstance)
            {
                errors.Add("Cannot validate attribute. Expected attribute type: File");
                return errors;
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