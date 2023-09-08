using CloudFabric.EAV.Domain.GeneratedValues;
using CloudFabric.EAV.Models;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Service.Abstractions;

public interface IGeneratedValueService<T> where T : IGeneratedValueInfo
{
    Task<T?> Create(Guid entityConfigurationId, AttributeConfigurationViewModel attributeConfiguration);

    Task<T?> Load(Guid entityConfigurationId, Guid attributeConfigurationId);

    Task<GeneratedValueActionResponse> Save(Guid entityConfigurationId, Guid attributeConfigurationId, T generatedValueInfo);
}
