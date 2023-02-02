namespace CloudFabric.EAV.Models.ViewModels.Attributes
{
    public class ValueFromListOptionViewModel
    {
        public string Name { get; set; }

        public string MachineName { get; set; }

        public Guid Id { get; set; }
    }

    public class ValueFromListAttributeConfigurationViewModel : AttributeConfigurationViewModel
    {
        public List<ValueFromListOptionViewModel> ValuesList { get; set; }
    }
}