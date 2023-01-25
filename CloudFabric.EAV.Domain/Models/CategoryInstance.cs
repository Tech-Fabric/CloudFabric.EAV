﻿using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.LocalEventSourcingPackages.Events.Category;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models
{
    public class CategoryInstance: AggregateBase
    {
        public override string PartitionKey => EntityConfigurationId.ToString();
        public Guid EntityConfigurationId { get; protected set; }
        public ReadOnlyCollection<AttributeInstance> Attributes { get; protected set; }
        public Guid? TenantId { get; protected set; }
        public string CategoryPath { get; protected set; }

        public CategoryInstance(IEnumerable<IEvent> events) : base(events)
        {
        }

        public CategoryInstance(Guid id, Guid entityConfigurationId, string categoryPath, ReadOnlyCollection<AttributeInstance> attributes, Guid? tenantId)
        {
            Apply(new CategoryCreated(id, entityConfigurationId.ToString(), entityConfigurationId, attributes, tenantId, categoryPath, DateTime.Now));
        }
        
        public async Task ChangeCategoryPath(string newCategoryPath, Guid childEntityConfigurationId)
        {
            Apply(new CategoryPathChanged(Id, EntityConfigurationId, CategoryPath, newCategoryPath));
        }
        
        public void On(CategoryCreated @event) {
            EntityConfigurationId = @event.EntityConfigurationId;
            Attributes = @event.Attributes;
            TenantId = @event.TenantId;
            CategoryPath = @event.CategoryPath;
            Id = @event.AggregateId;
        }
        
        public void On(CategoryPathChanged @event) {
            CategoryPath = @event.newCategoryPath;
        }
    }
}