﻿using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Data.Models.Attributes
{
    public class ImageAttributeInstance : AttributeInstance
    {
        public ImageAttributeValue Value { get; set; }

        public ImageAttributeInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public ImageAttributeInstance(string configurationAttributeMachineName) : base(configurationAttributeMachineName)
        {
        }
    }
}
