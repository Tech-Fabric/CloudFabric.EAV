using CloudFabric.EAV.Domain.Enums;
using CloudFabric.EAV.Domain.Models.Attributes;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Events.Configuration.Attributes
{
    public record ValueFromListConfigurationUpdated : Event
    {
        public ValueFromListConfigurationUpdated()
        {
        }

        public ValueFromListConfigurationUpdated(Guid id, ValueFromListAttributeType valueFromListAttributeType, List<ValueFromListOptionConfiguration> valueFromListOptions, string? attributeMachineNameToAffect)
        {
            AggregateId = id;
            ValueFromListAttributeType = valueFromListAttributeType;
            ValueFromListOptions = valueFromListOptions;
            AttributeMachineNameToAffect = attributeMachineNameToAffect;
        }

        public ValueFromListAttributeType ValueFromListAttributeType { get; set; }

        public List<ValueFromListOptionConfiguration> ValueFromListOptions { get; set; }

        public string? AttributeMachineNameToAffect { get; set; }
    }
}