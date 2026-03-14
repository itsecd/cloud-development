using Bogus;
using Bogus.DataSets;
using MedicalPatient.Generator.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace MedicalPatient.Generator.Services;

/// <summary>
/// Генератор данных для пациентов медицинской базы данных.
/// </summary>
public class MedicalPatientGenerator(ILogger<MedicalPatientGenerator> logger)
{
    private readonly Faker<MedicalPatientModel> _faker = new Faker<MedicalPatientModel>("ru")
            .RuleFor(x => x.FullName, GenerateFullName)
            .RuleFor(x => x.Address, f =>
                        $"г. {f.Address.City()}, ул. {f.Address.StreetName()}, д. {f.Address.BuildingNumber()}, кв. {f.Random.Int(1, 150)}"
                    )
            .RuleFor(x => x.BirthDate, f => f.Date.PastDateOnly(100))
            .RuleFor(x => x.Height, (f, x) => GenerateHeight(f, x.BirthDate))
            .RuleFor(x => x.Width, (f, x) => GenerateWidth(f, x.Height))
            .RuleFor(m => m.BloodType, f => f.Random.Int(1, 4))
            .RuleFor(m => m.RhFactor, f => f.Random.Bool())
            .RuleFor(m => m.LastInspectionDate, (f, m) => f.Date.BetweenDateOnly(m.BirthDate, DateOnly.FromDateTime(DateTime.Now)))
            .RuleFor(m => m.VaccinationMark, f => f.Random.Bool());

    /// <summary>
    /// Функия генерации случайного пациента с указанным идентификатором.
    /// </summary>
    /// <param name="id">Идентификатор пациента.</param>
    /// <returns>Сгенерированный объект <see cref="MedicalPatientModel"/> с заполненными полями.</returns>
    public MedicalPatientModel Generate(int id)
    {
        logger.LogInformation("Начало генерации медицинского пациента с ID: {Id}", id);
        var patient = _faker.Generate();
        patient.Id = id;

        return patient;
    }

    /// <summary>
    /// Функция генерации ФИО пациента с учетом гендера
    /// </summary>
    /// <param name="faker">Генератор случайных данных</param>
    /// <returns>Сгенерированное ФИО в виде единой строки</returns>
    private static string GenerateFullName(Faker faker)
    {
        var gender = faker.Person.Gender;
        var lastName = faker.Name.LastName(gender);
        var firstName = faker.Name.FirstName(gender);
        var patronymic = faker.Name.FirstName(Name.Gender.Male) + (gender == Name.Gender.Male ? "вич" : "вна");

        return string.Join(' ', firstName, lastName, patronymic);
    }

    /// <summary>
    /// Соотношение возраст - диапазон роста дл пациента
    /// </summary>
    /// <param name="age">Возраст пациента в годах</param>
    /// <returns>Минимальный и максимальный рост для данного возраста</returns>
    private static (double Min, double Max) HeightRangeByAge(double age)
    {
        return age switch
        {
            < 1 => (0.35, 0.80),
            < 3 => (0.80, 1.00),
            < 7 => (1.00, 1.30),
            < 13 => (1.30, 1.60),
            < 18 => (1.50, 1.90),
            _ => (1.50, 2.10)
        };
    }

    /// <summary>
    /// Функция генерации роста пациента на основе его возраста
    /// </summary>
    /// <param name="faker">Генератор случайных данных</param>
    /// <param name="birthDate">Дата рождения</param>
    /// <returns>Рост пациента, округленный до 2х знаков после запятой</returns>
    private static double GenerateHeight(Faker faker, DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var age = today.Year - birthDate.Year;

        if (birthDate > today.AddYears(-age))
        {
            age--;
        }

        var (minHeight, maxHeight) = HeightRangeByAge(age);

        return Math.Round(faker.Random.Double(minHeight, maxHeight), 2);
    }

    /// <summary>
    /// Функция генерации веса пациента на основе его роса с помощью индекса массы тела
    /// </summary>
    /// <param name="faker">Генератор случайных данных</param>
    /// <param name="height">Рост</param>
    /// <returns>Вес пациента, округленный до 2х знаков после запятой</returns>
    private static double GenerateWidth(Faker faker, double height)
    {
        var bmi = faker.Random.Double(15, 40);
        return (int)(bmi * height * height);
    }
}
