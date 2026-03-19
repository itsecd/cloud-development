using Domain.Contracts;
using Domain.Interfaces;

namespace Vehicle.Test;

/// <summary>
/// Класс для получения данных
/// </summary>
public class VehicleData
{
    private IVehicleContractGenerator _generator;

    public List<VehicleContractDto> InvalidVehicleContracts { get; set; } = new();
    public List<VehicleContractDto> VehicleContracts { get; set; } = new();
    public List<VehicleContractDto> ManualInvalidVehicleContracts { get; set; } = new();
    /// <summary>
    /// Генерация моковых данных
    /// </summary>
    /// <param name="generator">Генератор контракта</param>
    public VehicleData(IVehicleContractGenerator generator)
    {
        _generator = generator;

        for (var i = 1; i < 4; i++)
            InvalidVehicleContracts.Add(_generator.Generate(i));

        InvalidVehicleContracts[0].Year = 2600;
        InvalidVehicleContracts[1].Mileage = -123;
        InvalidVehicleContracts[2].LastServiceDate = new DateOnly(1888, 2, 1);

        for (var i = 0; i < 4; i++)
        {
            var id = Random.Shared.Next();
            VehicleContracts.Add(_generator.Generate(id));
        }


        ManualInvalidVehicleContracts =
            [
        // Год выпуска позже текущего
        new VehicleContractDto()
        {
              SystemId = 1,
              Vin = "NFCX3N18BZRN35445",
              Manufacturer = "Daihatsu",
              Model = "Charade",
              Year = 2600,
              BodyType = "Minivan",
              FuelType = "Gasoline",
              Color = "magenta",
              Mileage = 385802.0610109912,
              LastServiceDate = new DateOnly(2024,11,1)
           },

           // 2. Пробег меньше 0
           new VehicleContractDto()
           {
              SystemId = 2,
              Vin = "3BSG80R10IJA73277",
              Manufacturer = "Pontiac",
              Model = "Parisienne",
              Year = 1984,
              BodyType = "Coupe",
              FuelType = "Gasoline",
              Color = "blue",
              Mileage = -123,
              LastServiceDate = new DateOnly(2024,11,1)
           },
           //3. Дата последнего техобслуживания раньше года выпуска
           new VehicleContractDto()
           {
              SystemId = 3,
              Vin = "K69YCDITP1CW21110",
              Manufacturer = "Fiat",
              Model = "500 Abarth",
              Year = 1984,
              BodyType = "Coupe",
              FuelType = "Gasoline",
              Color = "blue",
              Mileage = -123,
              LastServiceDate = new DateOnly(2024,11,1)
           },
        ];

    }
}