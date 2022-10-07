using CloudFabric.EAV.Json.Utilities;

using System.Text.Json.Serialization;
using CloudFabric.EAV.Data.Models.Base;

namespace CloudFabric.EAV.Data.Models
{
    [JsonConverter(typeof(PolymorphicJsonConverter<AttributeInstance>))]
    public class AttributeInstance: Model 
    {
        public string ConfigurationAttributeMachineName { get; protected set; }
    }
}