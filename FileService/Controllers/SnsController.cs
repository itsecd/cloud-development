using Domain.Contracts;
using FileService.Models;
using FileService.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FileService.Controllers;

[ApiController]
[Route("sns")]
public class SnsController(
    IS3FileStorageService storageService,
    IHttpClientFactory httpClientFactory,
    ILogger<SnsController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [HttpPost]
    public async Task<IActionResult> HandleSnsMessage()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        SnsNotification? notification;
        try
        {
            notification = JsonSerializer.Deserialize<SnsNotification>(body, _jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize SNS notification");
            return BadRequest("Invalid SNS notification format");
        }

        if (notification is null)
            return BadRequest("Empty SNS notification");

        return notification.Type switch
        {
            "SubscriptionConfirmation" => await HandleSubscriptionConfirmationAsync(notification),
            "Notification" => await HandleNotificationAsync(notification),
            _ => Ok()
        };
    }

    private async Task<IActionResult> HandleSubscriptionConfirmationAsync(SnsNotification notification)
    {
        if (string.IsNullOrEmpty(notification.SubscribeURL))
        {
            logger.LogWarning("SubscriptionConfirmation received without SubscribeURL");
            return BadRequest("Missing SubscribeURL");
        }

        logger.LogInformation("Confirming SNS subscription for topic {TopicArn}", notification.TopicArn);

        var httpClient = httpClientFactory.CreateClient();
        var response = await httpClient.GetAsync(notification.SubscribeURL);
        response.EnsureSuccessStatusCode();

        logger.LogInformation("SNS subscription confirmed for topic {TopicArn}", notification.TopicArn);
        return Ok();
    }

    private async Task<IActionResult> HandleNotificationAsync(SnsNotification notification)
    {
        if (string.IsNullOrEmpty(notification.Message))
        {
            logger.LogWarning("SNS Notification received with empty Message");
            return BadRequest("Empty message");
        }

        VehicleContractDto? contract;
        try
        {
            contract = JsonSerializer.Deserialize<VehicleContractDto>(notification.Message, _jsonOptions);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Failed to deserialize VehicleContractDto from SNS message");
            return BadRequest("Invalid contract data");
        }

        if (contract is null)
        {
            logger.LogWarning("Deserialized null contract from SNS message");
            return BadRequest("Null contract");
        }

        var key = await storageService.SaveContractAsync(contract);
        logger.LogInformation("Contract {SystemId} saved to S3 at key {Key}", contract.SystemId, key);

        return Ok(new { Key = key });
    }
}
