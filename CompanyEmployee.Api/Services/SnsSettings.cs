namespace CompanyEmployee.Api.Services;

/// <summary>
/// Настройки SNS.
/// </summary>
public class SnsSettings
{
    public string ServiceURL { get; set; } = "http://localhost:4566";
    public string AccessKeyId { get; set; } = "test";
    public string SecretAccessKey { get; set; } = "test";
    public string Region { get; set; } = "us-east-1";
    public string TopicArn { get; set; } = "arn:aws:sns:us-east-1:000000000000:employee-events";
}