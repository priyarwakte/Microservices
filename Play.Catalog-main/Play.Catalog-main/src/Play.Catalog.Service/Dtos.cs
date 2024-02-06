using System;
using System.ComponentModel.DataAnnotations;

namespace Play.Catalog.Service.Dtos
{
    public record ItemDto(Guid Id, string Name, string Description, decimal Price, DateTimeOffset CreatedDate);

    public record CreateItemDto([Required] String Name, string Description, [Range(0, 1000)] decimal Price);

    public record UpdateItemDto([Required] String Name, string Description, [Range(0, 1000)] decimal Price);

}