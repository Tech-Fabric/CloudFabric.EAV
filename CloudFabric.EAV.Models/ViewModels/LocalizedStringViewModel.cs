namespace CloudFabric.EAV.Models.ViewModels;

public class LocalizedStringViewModel
{
    public int CultureInfoId { get; set; }

#pragma warning disable CA1720 // Identifier contains type name
    public string String { get; set; }
#pragma warning restore CA1720 // Identifier contains type name
}
