using System;
using System.Threading.Tasks;
using MassTransit;
using Play.Common;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers
{
    public class SubtractItemsConsumer : IConsumer<SubtractItems>
    {
        private readonly IRepository<InventoryItem> inventoryItemsRepository;
        private readonly IRepository<CatalogItem> catalogItemsRepository;

        public SubtractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
        {
            this.inventoryItemsRepository = inventoryItemsRepository;
            this.catalogItemsRepository = catalogItemsRepository;
        }

        public async Task Consume(ConsumeContext<SubtractItems> context)
        {
            var message = context.Message;

            var item = await catalogItemsRepository.GetAsync(message.CatalogItemId);

            if (item == null)
            {
                throw new UnknownItemException(message.CatalogItemId);
            }

            var inventoryItem = await inventoryItemsRepository.GetAsync(
                item => item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

            if (inventoryItem != null)
            {
                inventoryItem.Quantity -= message.Quantity;
                await inventoryItemsRepository.UpdateAsync(inventoryItem);
            }

            await context.Publish(new InventoryItemsSubtracted(message.CorrelationId));
        }
    }
}