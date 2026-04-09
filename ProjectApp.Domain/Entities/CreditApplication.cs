namespace ProjectApp.Domain.Entities;

public class CreditApplication
{
    public int Id { get; set; }
    public string CreditType { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public int TermMonths { get; set; }
    public double InterestRate { get; set; }
    public DateOnly ApplicationDate { get; set; }
    public bool RequiresInsurance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly? DecisionDate { get; set; }
    public decimal? ApprovedAmount { get; set; }
}
