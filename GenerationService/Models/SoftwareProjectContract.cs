namespace GenerationService.Models;


public record SoftwareProjectContract(

    int Id,
    /// <summary>Название проекта</summary>
    string ProjectName,
    /// <summary>Заказчик проекта</summary>
    string ClientCompany,
    /// <summary>ФИО менеджера проекта</summary>
    string ProjectManager,
    /// <summary>Дата начала</summary>
    DateOnly StartDate,
    /// <summary>Плановая дата завершения</summary>
    DateOnly PlannedEndDate,
    /// <summary>Фактическая дата завершения (null если проект не завершён)</summary>
    DateOnly? ActualEndDate,
    /// <summary>Бюджет</summary>
    decimal Budget,
    /// <summary>Фактические затраты</summary>
    decimal ActualCost,
    /// <summary>Процент выполнения от 0 до 100</summary>
    int CompletionPercentage
);