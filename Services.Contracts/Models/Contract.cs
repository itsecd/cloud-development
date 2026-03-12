namespace Services.Contracts.Models;

public class Contract
{
    public Guid Id { get; set; }

    public string ProjectName { get; set; } = "";

    public string Customer { get; set; } = "";

    public decimal Budget { get; set; }

    public DateTime CreatedAt { get; set; }
}