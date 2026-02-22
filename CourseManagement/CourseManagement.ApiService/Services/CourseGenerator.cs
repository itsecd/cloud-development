using Bogus;
using CourseManagement.ApiService.Dto;

namespace CourseManagement.ApiService.Services;

/// <summary>
/// Генератор для сущности типа Курс
/// </summary>
public class CourseGenerator
{
    /// <summary>
    /// Экземпляр генератора данных
    /// </summary>
    private readonly Faker<CourseDto> _courseFaker;

    /// <summary>
    /// Список названий курсов
    /// </summary>
    private readonly string[] _courseTitles = {
        "Разработка корпоративных приложений",
        "Алгоритмы и структуры данных",
        "Базы данных",
        "Веб-разработка",
        "Машинное обучение",
        "Основы информационной безопасности",
        "Математический анализ",
        "Линейная алгебра и геометрия",
        "Математическая статистика",
        "Теория вероятностей",
        "Физика",
        "Микроэкономика",
        "Макроэкономика",
        "Нейронные сети и глубокое обучение",
        "Сети и системы передачи информации",
        "Философия",
        "История России",
        "Дискретная математика",
        "Прикладное программирование",
        "Методы оптимизации",
        "Теория графов",
        "Технологии программирования",
        "Основы веб-приложений",
        "Безопасность вычислительных сетей",
        "Низкоуровневое программирование",
        "Системное программирование",
        "Криптография",
        "Криптопротоколы",
        "Жизненный цикл",
        "Цифровая обработка сигналов",
        "Цифровая обработка изображений",
        "Основы программиования",
        "Объектно-ориентированное программирование",
        "Форензика",
        "Компьютерная алгебра",
    };

    /// <summary>
    /// Конструктор для генератора
    /// </summary>
    public CourseGenerator()
    {
        _courseFaker = new Faker<CourseDto>("ru")
            .RuleFor(c => c.Title, f => f.PickRandom(_courseTitles))
            .RuleFor(c => c.Lector, f => f.Name.FullName())
            .RuleFor(c => c.StartDate, f => DateOnly.FromDateTime(f.Date.Future(1)))
            .RuleFor(c => c.EndDate, (f, c) => c.StartDate.AddMonths(f.Random.Int(1, 6)))
            .RuleFor(c => c.MaxStudents, f => f.Random.Int(10, 100))
            .RuleFor(c => c.EnrolledStudents, (f, c) => f.Random.Int(0, c.MaxStudents))
            .RuleFor(c => c.HasSertificate, f => f.Random.Bool())
            .RuleFor(c => c.Price, f => f.Finance.Amount(5000, 100000, 2))
            .RuleFor(c => c.Rating, f => f.Random.Int(1, 5));
    }

    /// <summary>
    /// Метод для генерации одного экземпляра сущности типа Курс 
    /// </summary>
    /// <param name="id">Идентификатор курса</param>
    /// <returns>Курс</returns>
    public CourseDto GenerateOne(int? id = null)
    {
        var course = _courseFaker.Generate();
        course.Id = id ?? new Randomizer().Int(1, 100000);
        return course;
    }
}