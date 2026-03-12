namespace CourseGenerator.Api.Models;

/// <summary>
/// Контракт на проведение учебного курса.
/// </summary>
/// <param name="Id">Идентификатор контракта.</param>
/// <param name="CourseName">Название курса.</param>
/// <param name="TeacherFullName">ФИО преподавателя.</param>
/// <param name="StartDate">Дата начала курса.</param>
/// <param name="EndDate">Дата окончания курса.</param>
/// <param name="MaxStudents">Максимальное число студентов.</param>
/// <param name="CurrentStudents">Текущее число студентов.</param>
/// <param name="HasCertificate">Признак выдачи сертификата по итогам курса.</param>
/// <param name="Price">Стоимость курса.</param>
/// <param name="Rating">Рейтинг курса.</param>
public sealed record CourseContract(
    int Id,
    string CourseName,
    string TeacherFullName,
    DateOnly StartDate,
    DateOnly EndDate,
    int MaxStudents,
    int CurrentStudents,
    bool HasCertificate,
    decimal Price,
    int Rating);