namespace Generator.Dto;

public class CreditOrderDto
{
    public int Id { get; set; }
    public string CreditType { get; set; } = "";
    public decimal RequestedSum { get; set; }
    public int MonthsDuration { get; set; }
    public double InterestRate { get; set; }
    public DateOnly FilingDate { get; set; }
    public bool IsInsuranceNeeded { get; set; }
    public string OrderStatus { get; set; } = "";
    public DateOnly? DecisionDate { get; set; }
    public decimal? ApprovedSum { get; set; }
}
