using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using ProjectApp.Domain.Entities;
using ProjectApp.Domain.Messaging;

namespace ProjectApp.IntegrationTests;

public sealed class BackendIntegrationTests
{
    [Fact]
    public async Task GeneratedPatient_IsStoredInObjectStorageByFileService()
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(6));
        var cancellationToken = cancellationTokenSource.Token;

        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ProjectApp_AppHost>(cancellationToken);

        await using var app = await appHost.BuildAsync(cancellationToken);
        await app.StartAsync(cancellationToken);

        await app.ResourceNotifications.WaitForResourceHealthyAsync("projectapp-apigateway", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("projectapp-api-0", cancellationToken);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("projectapp-fileservice", cancellationToken);

        using var gatewayClient = app.CreateHttpClient("projectapp-apigateway", "http");
        var patientId = Random.Shared.Next(100_000, 999_999);

        var patient = await GetFromJsonWithRetryAsync<MedicalPatient>(
            gatewayClient,
            $"/api/patient?id={patientId}",
            cancellationToken);

        Assert.NotNull(patient);
        Assert.Equal(patientId, patient.Id);
        Assert.False(string.IsNullOrWhiteSpace(patient.FullName));

        var storedMessage = await WaitForStoredPatientAsync(gatewayClient, patientId, cancellationToken);

        Assert.Equal(patient.Id, storedMessage.Patient.Id);
        Assert.Equal(patient.FullName, storedMessage.Patient.FullName);
        Assert.Equal(patient.BirthDate, storedMessage.Patient.BirthDate);
    }

    private static async Task<PatientGeneratedMessage> WaitForStoredPatientAsync(
        HttpClient gatewayClient,
        int patientId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var response = await gatewayClient.GetAsync($"/api/files/patients/{patientId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var storedMessage = await JsonSerializer.DeserializeAsync<PatientGeneratedMessage>(
                    stream,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web),
                    cancellationToken);

                Assert.NotNull(storedMessage);
                return storedMessage;
            }

            Assert.Contains(
                response.StatusCode,
                new[]
                {
                    HttpStatusCode.NotFound,
                    HttpStatusCode.BadGateway,
                    HttpStatusCode.ServiceUnavailable,
                    HttpStatusCode.InternalServerError
                });

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }

        throw new TimeoutException($"Patient file for id {patientId} was not stored in time.");
    }

    private static async Task<T?> GetFromJsonWithRetryAsync<T>(
        HttpClient httpClient,
        string requestUri,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(45);
        Exception? lastException = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                return await httpClient.GetFromJsonAsync<T>(requestUri, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        throw new TimeoutException($"GET {requestUri} did not succeed in time.", lastException);
    }
}
