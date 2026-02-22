using Bogus;
using Generator.DTO;

namespace Generator.Generator;

/// <summary>
///     Генератор объектов жилого строительства на основе Bogus.
/// </summary>
public class ResidentialBuildingGenerator(ILogger<ResidentialBuildingGenerator> logger)
{
    private const int MinBuildYear = 1900;

    private const double MinTotalArea = 10.0;

    private const double MaxTotalArea = 1000.0;

    private const double MinLivingAreaPartOfTotalArea = 0.5;

    private const double MaxLivingAreaPartOfTotalArea = 0.85;

    private const int MinTotalFloors = 1;

    private const int MaxTotalFloors = 100;

    private const double MinPricePerM2 = 35;

    private const double MaxPricePerM2 = 200;

    private static readonly string[] _propertyTypes =
    [
        "Квартира",
        "ИЖС",
        "Апартаменты",
        "Офис"
    ];

    private static readonly Faker<ResidentialBuildingDto>? _faker = new Faker<ResidentialBuildingDto>("ru")
        .RuleFor(x => x.Address, f => f.Address.FullAddress())
        .RuleFor(x => x.PropertyType, f => f.PickRandom(_propertyTypes))
        .RuleFor(x => x.BuildYear, f => f.Random.Int(MinBuildYear, DateTime.Today.Year))
        .RuleFor(x => x.TotalArea, f => Math.Round(f.Random.Double(MinTotalArea, MaxTotalArea), 2))
        .RuleFor(x => x.LivingArea, (f, dto) =>
        {
            var livingAreaPartOfTotalArea =
                f.Random.Double(MinLivingAreaPartOfTotalArea, MaxLivingAreaPartOfTotalArea);
            return Math.Round(livingAreaPartOfTotalArea * dto.TotalArea, 2);
        })
        .RuleFor(x => x.TotalFloors, f => f.Random.Int(MinTotalFloors, MaxTotalFloors))
        .RuleFor(x => x.Floor, (f, dto) =>
        {
            if (dto.PropertyType is "ИЖС")
            {
                return null;
            }

            return f.Random.Int(1, dto.TotalFloors);
        })
        .RuleFor(x => x.CadastralNumber, f =>
            $"{f.Random.Int(1, 99):D2}:" +
            $"{f.Random.Int(1, 99):D2}:" +
            $"{f.Random.Int(1, 9999999):D7}:" +
            $"{f.Random.Int(1, 9999):D4}")
        .RuleFor(x => x.CadastralValue, (f, dto) =>
        {
            var pricePerM2 = f.Random.Double(MinPricePerM2, MaxPricePerM2);
            var price = dto.TotalArea * pricePerM2;
            return (decimal)Math.Round(price, 2);
        });
    
    /// <summary>
    ///     Генерирует объект жилого строительства для заданного идентификатора.
    /// </summary>
    /// <param name="id">Идентификатор объекта жилого строительства.</param>
    /// <returns>Сгенерированный объект жилого строительства.</returns>
    public ResidentialBuildingDto Generate(int id)
    {
        logger.LogInformation("Generating Residential Building for Id={id}", id);

        ResidentialBuildingDto? generatedObject = _faker!.Generate();
        generatedObject.Id = id;

        logger.LogInformation(
            "Residential building generated: Id={Id}, Address='{Address}', PropertyType='{PropertyType}', " +
            "BuildYear={BuildYear}, TotalArea={TotalArea}, LivingArea={LivingArea}, Floor={Floor}, " +
            "TotalFloors={TotalFloors}, CadastralNumber='{CadastralNumber}', CadastralValue={CadastralValue}",
            generatedObject.Id, generatedObject.Address, generatedObject.PropertyType, generatedObject.BuildYear,
            generatedObject.TotalArea, generatedObject.LivingArea, generatedObject.Floor, generatedObject.TotalFloors,
            generatedObject.CadastralNumber, generatedObject.CadastralValue
        );

        return generatedObject;
    }
}