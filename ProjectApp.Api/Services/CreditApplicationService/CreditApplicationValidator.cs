using ProjectApp.Domain.Entities;

namespace ProjectApp.Api.Services.CreditApplicationService;

public class CreditApplicationValidator
{
    private static readonly HashSet<string> TerminalStatuses = ["Одобрена", "Отклонена"];

    public bool TryValidate(CreditApplication application, out string error)
    {
        if (string.IsNullOrWhiteSpace(application.CreditType))
        {
            error = "CreditType is empty.";
            return false;
        }

        if (application.InterestRate < 0)
        {
            error = "InterestRate cannot be negative.";
            return false;
        }

        if (application.ApprovedAmount.HasValue && application.ApprovedAmount.Value > application.RequestedAmount)
        {
            error = "ApprovedAmount cannot be greater than RequestedAmount.";
            return false;
        }

        if (TerminalStatuses.Contains(application.Status))
        {
            if (application.DecisionDate is null)
            {
                error = "DecisionDate must be set for terminal statuses.";
                return false;
            }

            if (application.DecisionDate <= application.ApplicationDate)
            {
                error = "DecisionDate must be later than ApplicationDate.";
                return false;
            }
        }
        else
        {
            if (application.DecisionDate is not null)
            {
                error = "DecisionDate must be null for non-terminal statuses.";
                return false;
            }

            if (application.ApprovedAmount is not null)
            {
                error = "ApprovedAmount must be null for non-terminal statuses.";
                return false;
            }
        }

        if (application.Status == "Одобрена" && application.ApprovedAmount is null)
        {
            error = "ApprovedAmount must be set for status 'Одобрена'.";
            return false;
        }

        if (application.Status == "Отклонена" && application.ApprovedAmount is not null)
        {
            error = "ApprovedAmount must be null for status 'Отклонена'.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
