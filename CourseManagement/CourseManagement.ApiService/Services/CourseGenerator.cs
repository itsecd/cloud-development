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
    /// Шаблоны курсов
    /// </summary>
    private readonly CourseTemplate[] _courseTemplates =
    {
        new() {
            Title = "разработка корпоративных приложений",
            Description = "Изучение основ C# и платформы .NET, реализация сервисно-ориентированного приложения, работа с REST API, Entity Framework Core.",
            Department = "Кафедра геоинформатики и информационной безопасности",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Алгоритмы и структуры данных",
            Description = "Базовые алгоритмы сортировки, поиска, структуры данных: стеки, очереди, деревья, графы. Анализ сложности алгоритмов.",
            Department = "Кафедра геоинформатики и информационной безопасности",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Базы данных",
            Description = "Проектирование БД, нормализация, SQL запросы, транзакции, индексы. Работа с MySQL, PostgreSQL и MongoDB.",
            Department = "Кафедра геоинформатики и информационной безопасности",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Веб-разработка",
            Description = "Изучение основ построения веб-приложений, знакомство с React, Bootstrap, JS.",
            Department = "Кафедра геоинформатики и информационной безопасности",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Машинное обучение",
            Description = "Основы ML: оптимальные классификаторы, линейные классификаторы, деревья решений, кластеризация.",
            Department = "Кафедра геоинформатики и информационной безопасности",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Основы информационной безопасности",
            Description = "Кртакий экскурс в криптографию, сетевые атаки, защиту веб-приложений, принципы безопасной разработки, анализ уязвимостей.",
            Department = "Кафедра геоинформатики и информационной безопасности",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Математический анализ",
            Description = "Основы математического анализа.",
            Department = "Кафедра прикланой математики и информатики",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Линейная алгебра и геометрия",
            Description = "Основы линейной алгебры и геометрии, векторы, матрицы, линейные пространства, операторы, функционалы.",
            Department = "Кафедра прикланой математики и информатики",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Математическая статистика",
            Description = "Основы математической статистики, реализации случайных величин, точечные и интервальные оценки.",
            Department = "Кафедра прикланой математики и информатики",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Теория вероятностей",
            Description = "Основы теории вероятностей, вероятности, дискретные и непрерывные случайные величины, функция/плотность распределения моменты.",
            Department = "Кафедра прикланой математики и информатики",
            Faculty = "Факультет информатики и кибернетики"
        },
        new() {
            Title = "Физика",
            Description = "Электродинамика, электро-магнитное взаимодействие, уравнения Максвелла.",
            Department = "Кафедра физики",
            Faculty = "Факультет физики"
        },
        new() {
            Title = "Микроэкономика",
            Description = "Спрос и предложение, теория потребительского выбора, издержки производства, рыночные структуры.",
            Department = "Кафедра экономической теории",
            Faculty = "Факультет экономики"
        },
        new() {
            Title = "Макроэкономика",
            Description = "ВВП, инфляция, безработица, денежно-кредитная политика, экономический рост, международная торговля.",
            Department = "Кафедра экономической теории",
            Faculty = "Факультет экономики"
        }
    };

    /// <summary>
    /// Конструктор для генератора
    /// </summary>
    public CourseGenerator()
    {
        _courseFaker = new Faker<CourseDto>("ru")
            .CustomInstantiator(f =>
            {
                var template = f.PickRandom(_courseTemplates);

                return new CourseDto
                {
                    Title = template.Title,
                    Description = template.Description,
                    Department = template.Department,
                    Faculty = template.Faculty,
                    Lector = f.Name.FullName(),

                    LecturesCount = f.Random.Int(8, 24),
                    PracticesCount = f.Random.Int(4, 20),
                    LaboratoriesCount = f.Random.Int(0, 10),

                    StartDate = f.Date.Future(1),
                    EndDate = f.Date.Future(1, DateTime.Now.AddMonths(6)),

                    MaxStudents = f.Random.Int(15, 80),
                    EnrolledStudents = f.Random.Int(0, 50),

                    Status = f.PickRandom("Планируется", "Идёт набор", "В процессе", "Завершён"),
                    Level = f.PickRandom("Начальный", "Средний", "Продвинутый"),
                    Price = f.Finance.Amount(15000, 120000),
                    Format = f.PickRandom("Онлайн", "Офлайн", "Смешанный")
                };
            })
            .RuleFor(c => c.TotalHours, (f, c) => c.LecturesCount * 1.5 + c.PracticesCount * 1.5 + c.LaboratoriesCount * 3.0)
            .RuleFor(c => c.EnrolledStudents, (f, c) => f.Random.Int(0, c.MaxStudents));
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

    /// <summary>
    /// Класс для сущности типа Шаблон курса 
    /// </summary>
    private class CourseTemplate
    {
        /// <summary>
        /// Название курса
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Описание курса
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Кафедра
        /// </summary>
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Факультет
        /// </summary>
        public string Faculty { get; set; } = string.Empty;
    }
}