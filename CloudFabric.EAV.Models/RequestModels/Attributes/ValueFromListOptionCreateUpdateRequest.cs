namespace CloudFabric.EAV.Models.RequestModels.Attributes
{
    public class ValueFromListOptionCreateUpdateRequest
    {

        public ValueFromListOptionCreateUpdateRequest(string machineName, string name, object? valueToAppend)
        {
            MachineName = machineName;
            Name = name;
            ValueToAppend = valueToAppend;
        }

        public string MachineName { get; set; }
        public string Name { get; set; }
        public object? ValueToAppend { get; set; }
    }
}