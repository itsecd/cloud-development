using Amazon.S3.Model;
using Aspire.Hosting.Testing;
using MedicalPatient.Generator.Models;
using System.Net;
using System.Text.Json;
using Xunit;
using System.Net.Http.Json;


namespace MedicalPatient.Tests;

/// <summary>
/// Интеграционные тесты, проверяющие корректную совместную работу всех сервисов.
/// </summary>
public class Tests(Fixture fixture) : IClassFixture<Fixture>
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Тест на верность генерации данных о пациенте
    /// </summary>
    [Fact]
    public async Task GetPatient_TestValid()
    {
        using var client = fixture.App.CreateHttpClient("medicalpatient-apigateway", "http");

        using var response = await client.GetAsync("/medicalpatient-generator?id=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var patient = await response.Content.ReadFromJsonAsync<MedicalPatientModel>(_jsonOptions);

        Assert.NotNull(patient);
        Assert.Equal(1, patient.Id);
        Assert.False(string.IsNullOrEmpty(patient.FullName));
        Assert.False(string.IsNullOrEmpty(patient.Address));
        Assert.True(patient.BirthDate != default);
        Assert.True(patient.LastInspectionDate != default);

        Assert.InRange(patient.Height, 0.00000001, double.MaxValue);
        Assert.InRange(patient.Weight, 0.00000001, double.MaxValue);

        Assert.True(patient.BloodType >= 1 && patient.BloodType <= 4);

        Assert.True(patient.BirthDate < patient.LastInspectionDate);
    }

    /// <summary>
    /// Тест на однковых генерации данных о пациенте с одинаковым Id
    /// </summary>
    [Fact]
    public async Task GetPatient_CheckPatientsWithEqualIDs()
    {
        var randomId = Random.Shared.Next(10, 20);
        using var client = fixture.App.CreateHttpClient("medicalpatient-apigateway", "http");

        var patient = await client.GetFromJsonAsync<MedicalPatientModel>($"/medicalpatient-generator?id={randomId}", _jsonOptions);
        var patientEqualId = await client.GetFromJsonAsync<MedicalPatientModel>($"/medicalpatient-generator?id={randomId}", _jsonOptions);

        Assert.NotNull(patient);
        Assert.NotNull(patientEqualId);

        Assert.Equal(patient.Id, patientEqualId.Id);
        Assert.Equal(patient.FullName, patientEqualId.FullName);
        Assert.Equal(patient.Address, patientEqualId.Address);
        Assert.Equal(patient.BirthDate.ToString(), patientEqualId.BirthDate.ToString());
        Assert.Equal(patient.LastInspectionDate.ToString(), patientEqualId.LastInspectionDate.ToString());
        Assert.Equal(patient.Height, patientEqualId.Height);
        Assert.Equal(patient.Weight, patientEqualId.Weight);
        Assert.Equal(patient.BloodType, patientEqualId.BloodType);
        Assert.Equal(patient.RhFactor, patientEqualId.RhFactor);
        Assert.Equal(patient.VaccinationMark, patientEqualId.VaccinationMark);
    }

    /// <summary>
    /// Тест для проверки не совпадения объектов сгенерированных с разными Id
    /// </summary>
    [Fact]
    public async Task GetPatient_CheckPatientsWithNotEqualIDs()
    {
        using var client = fixture.App.CreateHttpClient("medicalpatient-apigateway", "http");

        var patient1 = await client.GetFromJsonAsync<MedicalPatientModel>("/medicalpatient-generator?id=1", _jsonOptions);
        var patient2 = await client.GetFromJsonAsync<MedicalPatientModel>("/medicalpatient-generator?id=2", _jsonOptions);

        Assert.NotNull(patient1);
        Assert.NotNull(patient2);
        Assert.Equal(1, patient1.Id);
        Assert.Equal(2, patient2.Id);
        Assert.NotEqual(patient1.FullName, patient2.FullName);
        Assert.NotEqual(patient1.Address, patient2.Address);
    }

    /// <summary>
    /// Проверка валидации: при id <= 0 API должно вернуть 400.
    /// </summary>
    [Fact]
    public async Task GetPatient_InvalidId_ReturnsBadRequest()
    {
        using var client = fixture.App.CreateHttpClient("medicalpatient-apigateway", "http");
        using var response = await client.GetAsync("/medicalpatient-generator?id=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Проверка end-to-end: генератор -> очередь -> FileService -> MinIO.
    /// </summary>
    [Fact]
    public async Task GetPatient_WritesJsonToMinio()
    {
        var id = Random.Shared.Next(1000, 2000);
        var key = $"patient-{id}.json";

        using var client = fixture.App.CreateHttpClient("medicalpatient-apigateway", "http");
        using var response = await client.GetAsync($"/medicalpatient-generator?id={id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var bucketCandidates = new[] { "medical-patient", "medical-patients", "medical-patinet" };
        string? bucket = null;

        foreach (var candidate in bucketCandidates)
        {
            if (await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(fixture.S3Client, candidate))
            {
                bucket = candidate;
                break;
            }
        }

        Assert.False(string.IsNullOrWhiteSpace(bucket));

        var found = false;
        for (var i = 0; i < 15 && !found; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(2));

            var list = await fixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
            {
                BucketName = bucket!,
                Prefix = key
            });

            found = list.S3Objects.Any(x => x.Key == key);
        }

        Assert.True(found, $"Object '{key}' was not found in bucket '{bucket}'.");
    }
}
