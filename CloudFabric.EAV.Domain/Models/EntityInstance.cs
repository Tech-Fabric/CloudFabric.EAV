using System.Collections.ObjectModel;

using CloudFabric.EAV.Domain.Events.Instance.Attribute;
using CloudFabric.EAV.Domain.Events.Instance.Entity;
using CloudFabric.EventSourcing.Domain;
using CloudFabric.EventSourcing.EventStore;

namespace CloudFabric.EAV.Domain.Models;

public class EntityInstance : AggregateBase
{
    /// <summary>
    /// For multi-tenant applications this can be used to separate records between tenants.
    /// </summary>
    public Guid? TenantId { get; protected set; }

    /// <summary>
    /// Unique human-readable identifier. Can be used in urls to help with SEO.
    /// </summary>
    public string? MachineName { get; protected set; }

    public override string PartitionKey => EntityConfigurationId.ToString();

    /// <summary>
    /// An entity instance can belong to many hierarchy trees, this list will contain a record for each hierarchy tree
    /// and a place (path) in it.
    /// </summary>
    public IReadOnlyCollection<CategoryPath> CategoryPaths { get; protected set; } =
        new ReadOnlyCollection<CategoryPath>(new List<CategoryPath>());

    /// <summary>
    /// All attributes for this EntityInstance are validated against EntityConfiguration's attributes.
    /// </summary>
    public Guid EntityConfigurationId { get; protected set; }

    public IReadOnlyCollection<AttributeInstance> Attributes { get; protected set; } =
        new ReadOnlyCollection<AttributeInstance>(new List<AttributeInstance>());

    protected EntityInstance()
    {
    }

    public EntityInstance(IEnumerable<IEvent> events) : base(events)
    {
    }

    public EntityInstance(
        Guid id,
        Guid entityConfigurationId,
        List<AttributeInstance> attributes,
        string? machineName,
        Guid? tenantId,
        List<CategoryPath>? categoryPaths
    ) {
        Apply(new EntityInstanceCreated(
            id,
            entityConfigurationId,
            attributes,
            machineName,
            tenantId,
            categoryPaths
        ));
    }

    public void AddAttributeInstance(AttributeInstance attribute)
    {
        Apply(new AttributeInstanceAdded(Id, attribute));
    }

    public void UpdateAttributeInstance(AttributeInstance attribute)
    {
        Apply(new AttributeInstanceUpdated(Id, EntityConfigurationId, attribute));
    }

    public void RemoveAttributeInstance(string attributeMachineName)
    {
        Apply(new AttributeInstanceRemoved(Id, EntityConfigurationId, attributeMachineName));
    }

    public void UpdateMachineName(string newMachineName)
    {
        Apply(new EntityInstanceMachineNameUpdated(Id, EntityConfigurationId, newMachineName));
    }

    /// <summary>
    /// Updates the path of this entity inside one of hierarchy trees is belongs to.
    /// </summary>
    /// <param name="treeId">Id of existing category tree.</param>
    /// <param name="categoryPath">Slash-separated path constructed from `machineName`s of parent EntityInstances.
    /// Empty for root node. Example: Electronics/Laptops </param>
    /// <param name="parentId"></param>
    public void UpdateCategoryPath(Guid treeId, string categoryPath, Guid parentId)
    {
        Apply(new EntityInstanceCategoryPathUpdated(Id, EntityConfigurationId, treeId, categoryPath, parentId));
    }

    #region Event Handlers

    public void On(EntityInstanceCreated @event)
    {
        Id = @event.AggregateId;
        EntityConfigurationId = @event.EntityConfigurationId;
        Attributes = new List<AttributeInstance>(@event.Attributes).AsReadOnly();
        MachineName = @event.MachineName;
        TenantId = @event.TenantId;
        CategoryPaths = @event.CategoryPaths ?? new List<CategoryPath>().AsReadOnly();
    }

    public void On(AttributeInstanceAdded @event)
    {
        List<AttributeInstance> newCollection = new List<AttributeInstance>(Attributes) { @event.AttributeInstance };
        Attributes = newCollection.AsReadOnly();
    }

    public void On(AttributeInstanceUpdated @event)
    {
        AttributeInstance? attribute = Attributes.FirstOrDefault(x =>
            x.ConfigurationAttributeMachineName == @event.AttributeInstance.ConfigurationAttributeMachineName
        );

        if (attribute != null)
        {
            var newCollection = new List<AttributeInstance>(Attributes);

            newCollection.Remove(attribute);
            newCollection.Add(@event.AttributeInstance);

            Attributes = newCollection.AsReadOnly();
        }
    }

    public void On(AttributeInstanceRemoved @event)
    {
        AttributeInstance? attribute = Attributes.FirstOrDefault(x =>
            x.ConfigurationAttributeMachineName == @event.AttributeMachineName
        );

        if (attribute != null)
        {
            var newCollection = new List<AttributeInstance>(Attributes);

            newCollection.Remove(attribute);

            Attributes = newCollection.AsReadOnly();
        }
    }

    public void On(EntityInstanceCategoryPathUpdated @event)
    {
        CategoryPath? categoryPath = CategoryPaths.FirstOrDefault(x => x.TreeId == @event.CategoryTreeId);

        var newCollection = new List<CategoryPath>(CategoryPaths);

        if (categoryPath != null)
        {
            newCollection.Remove(categoryPath);
        }

        var newCategoryPath = categoryPath != null
            ? categoryPath with
            {
                Path = @event.CategoryPath,
                ParentMachineName = @event.ParentMachineName,
                ParentId = @event.ParentId
            }
            : new CategoryPath
            {
                TreeId = @event.CategoryTreeId,
                Path = @event.CategoryPath,
                ParentId = @event.ParentId,
                ParentMachineName = @event.ParentMachineName
            };

        newCollection.Add(newCategoryPath);

        CategoryPaths = newCollection.AsReadOnly();
    }

    public void On(EntityInstanceMachineNameUpdated @event)
    {
        MachineName = @event.NewMachineName;
    }

    #endregion
}
