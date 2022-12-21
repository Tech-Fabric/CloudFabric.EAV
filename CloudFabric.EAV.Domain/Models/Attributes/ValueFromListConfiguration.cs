using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Base;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ValueFromListConfiguration : AttributeConfiguration
    {

        public ValueFromListConfiguration(IEnumerable<IEvent> events) : base(events)
        {
        }
        public ValueFromListConfiguration(Guid id,
            string machineName,
            List<LocalizedString> name,
            EavAttributeType valueType,
            List<LocalizedString> description = null,
            bool isRequired = false,
            Guid? TenantId = null) : base(id, machineName, name, valueType, description, isRequired, TenantId)
        {
        }
        public override EavAttributeType ValueType => EavAttributeType.OneValueFromList;

        public EavAttributeType ItemsType { get; protected set; }

        public Guid ItemsAttributeConfigurationId { get; protected set; }
    }
}