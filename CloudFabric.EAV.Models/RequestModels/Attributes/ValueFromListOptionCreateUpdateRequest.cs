namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ValueFromListOptionCreateUpdateRequest
    {
        public ValueFromListOptionCreateUpdateRequest(string name, string? machineName)
        {
            MachineName = machineName;
            Name = name;
        }

        public string Name { get; set; }

        public string? MachineName { get; set; }
    }
}
