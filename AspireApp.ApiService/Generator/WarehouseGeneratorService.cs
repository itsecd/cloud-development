using AspireApp.ApiService.Entities;
using AspireApp.ApiService.Messaging;

namespace AspireApp.ApiService.Generator;

/// <summary>
/// Служба для запуска юзкейса по обработке товаров на складе
/// </summary>
/// <param name="warehouseCache">Служба кэширования</param>
/// <param name="producer">Служба-публикатор сообщений в SQS</param>
/// <param name="logger">Логгер</param>
/// <param name="generator">Генератор товаров</param>
public class WarehouseGeneratorService(
    IWarehouseCache warehouseCache,
    SqsProducerService producer,
    ILogger<WarehouseGeneratorService> logger,
    WarehouseGenerator generator) : IWarehouseGeneratorService
{
    /// <inheritdoc/>
    public async Task<Warehouse> ProcessWarehouse(int id)
    {
        logger.LogInformation("Обработка товара с Id = {Id} начата", id);

        // Получаем товар из кэша
        Warehouse? warehouse;
        try
        {
            warehouse = await warehouseCache.GetAsync(id);
            if (warehouse != null)
            {
                logger.LogInformation("Товар {Id} получен из кэша", id);
                return warehouse;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось получить товар {Id} из кэша (ошибка игнорируется)", id);
        }

        // Если в кэше нет или ошибка — генерируем новый товар
        logger.LogInformation("Товар {Id} в кэше не найден или кэш недоступен, запуск генерации", id);
        warehouse = generator.Generate();
        warehouse.Id = id;

        // Отправка в брокер только при генерации нового объекта
        await producer.SendMessage(warehouse);

        // Попытка сохранить в кэш
        try
        {
            logger.LogInformation("Сохранение товара {Id} в кэш", id);
            await warehouseCache.SetAsync(warehouse);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось сохранить товар {Id} в кэш (ошибка игнорируется)", id);
        }

        return warehouse;
    }
}
