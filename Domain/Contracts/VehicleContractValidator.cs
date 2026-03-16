namespace Domain.Contracts;

/// <summary>
/// Класс Для валидации данных 
/// </summary>
public class VehicleContractValidator
{
    /// <summary>
    /// Валидация данных 
    /// Год выпуска не может быть позже текущего
    /// Пробег не может быть меньше нуля
    /// Дата последнего техобслуживания не может быть раньше года выпуска
    /// Не пустые поля 
    /// </summary>
    /// <param name="dto"></param>
    /// <exception cref="ArgumentException"></exception>
    public static void Validate(VehicleContractDto dto)
    {
        if (dto.Year > DateTime.UtcNow.Year)
            throw new ArgumentException("Year cannot be greater than current year.");

        if (dto.Mileage < 0)
            throw new ArgumentException("Mileage cannot be negative.");

        var minServiceDate = new DateOnly(dto.Year, 1, 1);

        if (dto.LastServiceDate < minServiceDate)
            throw new ArgumentException("LastServiceDate cannot be earlier than 01.01.Year.");

        if (string.IsNullOrWhiteSpace(dto.Vin))
            throw new ArgumentException("Vin is required.");

        if (string.IsNullOrWhiteSpace(dto.Manufacturer))
            throw new ArgumentException("Manufacturer is required.");

        if (string.IsNullOrWhiteSpace(dto.Model))
            throw new ArgumentException("Model is required.");

        if (string.IsNullOrWhiteSpace(dto.BodyType))
            throw new ArgumentException("BodyType is required.");

        if (string.IsNullOrWhiteSpace(dto.FuelType))
            throw new ArgumentException("FuelType is required.");

        if (string.IsNullOrWhiteSpace(dto.Color))
            throw new ArgumentException("Color is required.");
    }
    /// <summary>
    /// Валидация данных 
    /// Год выпуска не может быть позже текущего
    /// Пробег не может быть меньше нуля
    /// Дата последнего техобслуживания не может быть раньше года выпуска
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    public static bool ValidateBool(VehicleContractDto dto)
    {
        var minServiceDate = new DateOnly(dto.Year, 1, 1);
        if (dto.Year > DateTime.UtcNow.Year || dto.Mileage < 0 || dto.LastServiceDate < minServiceDate)
            return false;

        if (string.IsNullOrWhiteSpace(dto.Vin) || string.IsNullOrWhiteSpace(dto.Manufacturer) || 
            string.IsNullOrWhiteSpace(dto.Model) || string.IsNullOrWhiteSpace(dto.BodyType) ||
            string.IsNullOrWhiteSpace(dto.FuelType) || string.IsNullOrWhiteSpace(dto.Color))
            return false;
        return true;
    }
}
