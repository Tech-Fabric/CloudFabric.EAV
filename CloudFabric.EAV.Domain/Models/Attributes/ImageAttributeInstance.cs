﻿using System.Collections.Generic;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models.Attributes
{
    public class ImageAttributeInstance : AttributeInstance
    {
        public ImageAttributeValue Value { get; set; }
    }
}