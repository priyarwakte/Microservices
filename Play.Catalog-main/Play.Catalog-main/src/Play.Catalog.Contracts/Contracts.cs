using System;

namespace Play.catalog.contracts
{
    public record CatalogItemCreated(Guid ItemId, string Name, string Description, decimal price);

    public record CatalogItemUpdated(Guid ItemId, string Name, string Description, decimal price);

    public record CatalogItemDeleted(Guid ItemId);
}