using WarehouseItem.Generator.DTO;

namespace WarehouseItem.Generator.Service;

public interface IWarehouseItemService
{
    public Task<WarehouseItemDto> GetAsync(int id, CancellationToken cancellationToken = default);
}
