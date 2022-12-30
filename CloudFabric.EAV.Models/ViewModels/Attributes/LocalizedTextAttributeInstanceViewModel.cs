using CloudFabric.EAV.Models.ViewModels;
using CloudFabric.EAV.Models.ViewModels.Attributes;

namespace CloudFabric.EAV.Models.RequestModels.Attributes;

public class LocalizedTextAttributeInstanceViewModel : AttributeInstanceViewModel
{
    public List<LocalizedStringViewModel> Value { get; set; }
}