namespace Service.Api.Entity;
/// <summary>
/// This class describes program project with customer, manager,
/// dates of start n end, budged and spent money.
/// </summary>
public record ProgramProject
{
    /// <summary>
    /// The project's ID
    /// </summary>
    public int Id { get; init;  }
    /// <summary>
    /// The project's name
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// The project's customer
    /// </summary>
    public string Customer { get; set; }
    /// <summary>
    /// The project's manager
    /// </summary>
    public string Manager { get; set; }
    /// <summary>
    /// The date of actual start of the project
    /// </summary>
    public DateOnly StartDate { get; set; }
    /// <summary>
    /// The planned date of the end of project
    /// </summary>
    public DateOnly EndDatePlanned { get; set; }
    /// <summary>
    /// The real date of the end of the project. It may be null if the project is still in progress.
    /// </summary>
    public DateOnly? EndDateReal { get; set; }
    /// <summary>
    /// The project's budget
    /// </summary>
    public decimal Budget { get; set; }
    /// <summary>
    /// How much money the project actually spent
    /// </summary>
    public decimal SpentMoney { get; set; }
    /// <summary>
    /// Shows how complete the project is
    /// </summary>
    public int FinishedPerCent { get; set; }
}
