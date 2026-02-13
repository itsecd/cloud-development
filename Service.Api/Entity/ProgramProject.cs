namespace Service.Api.Entity;

public class ProgramProject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Customer { get; set; }
    public string Manager { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDatePlanned { get; set; }
    public DateOnly? EndDateReal { get; set; }
    public decimal Budget { get; set; }
    public decimal SpentMoney { get; set; }
    public int FinishedPerCent { get; set; }
}
