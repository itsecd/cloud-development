using Amazon.S3.Model;
using Aspire.Hosting.Testing;
using CompanyEmployee.ApiService.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CompanyEmployee.Test;

public class IntegrationTests : IClassFixture<Fixture>
{
    private readonly Fixture _fixture;
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public IntegrationTests(Fixture fixture)
    {
        _fixture = fixture;
    }
    
    /// <summary>
    /// Тест на корректное создание сотрудника
    /// </summary>
    [Fact]
    public async Task GetEmployee_GenerateTest()
    {
        var client = _fixture.App.CreateHttpClient(
            "companyemployee-apigateway",
            "gateway");

        HttpResponseMessage? response = null;

        response = await client.GetAsync("/api/CompanyEmployee?id=1");

        Assert.NotNull(response);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var employee = await response.Content.ReadFromJsonAsync<CompanyEmployeeModel>(_jsonOptions);

        Assert.NotNull(employee);
        Assert.Equal(1, employee.Id);
        Assert.False(string.IsNullOrWhiteSpace(employee.FullName));
        Assert.False(string.IsNullOrWhiteSpace(employee.Email));
        Assert.False(string.IsNullOrWhiteSpace(employee.PhoneNumber));
        Assert.False(string.IsNullOrWhiteSpace(employee.Email));
        Assert.False(string.IsNullOrWhiteSpace(employee.JobTitle));
        Assert.False(string.IsNullOrWhiteSpace(employee.Department));
        Assert.True(employee.Salary > 0);
        Assert.Equal(employee.Dismissal, employee.DismissalDate is not null);
    }

    /// <summary>
    /// Тест, что при запросах с одинаковыми id приходят одинаковые сотрудники
    /// </summary>
    [Fact]
    public async Task GetEmployee_CashTest()
    {
        var client = _fixture.App.CreateHttpClient(
            "companyemployee-apigateway",
            "gateway");

        var response1 = await client.GetAsync("/api/CompanyEmployee?id=2");
        var employee1 = await response1.Content.ReadFromJsonAsync<CompanyEmployeeModel>(_jsonOptions);

        var response2 = await client.GetAsync("/api/CompanyEmployee?id=2");
        var employee2 = await response2.Content.ReadFromJsonAsync<CompanyEmployeeModel>(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(employee1);
        Assert.NotNull(employee2);
        Assert.Equal(employee1.Id, employee2.Id);
        Assert.Equal(employee1.FullName, employee2.FullName);
        Assert.Equal(employee1.Email, employee2.Email);
        Assert.Equal(employee1.PhoneNumber, employee2.PhoneNumber);
        Assert.Equal(employee1.Salary, employee2.Salary);
        Assert.Equal(employee1.Dismissal, employee2.Dismissal);
        Assert.Equal(employee1.DismissalDate, employee2.DismissalDate);
    }

    /// <summary>
    /// Тест, что при запросах с разными id приходят разные сотрудники
    /// </summary>
    [Fact]
    public async Task GetEmployee_DiferentEmployeeTest()
    {
        var client = _fixture.App.CreateHttpClient(
            "companyemployee-apigateway",
            "gateway");

        var response1 = await client.GetAsync("/api/CompanyEmployee?id=3");
        var employee1 = await response1.Content.ReadFromJsonAsync<CompanyEmployeeModel>(_jsonOptions);

        var response2 = await client.GetAsync("/api/CompanyEmployee?id=4");
        var employee2 = await response2.Content.ReadFromJsonAsync<CompanyEmployeeModel>(_jsonOptions);

        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        Assert.NotNull(employee1);
        Assert.NotNull(employee2);
        Assert.NotEqual(employee1.Id, employee2.Id);
        Assert.NotEqual(employee1.FullName, employee2.FullName);
        Assert.NotEqual(employee1.Email, employee2.Email);
        Assert.NotEqual(employee1.PhoneNumber, employee2.PhoneNumber);
        Assert.NotEqual(employee1.Salary, employee2.Salary);
        Assert.NotEqual(employee1.Dismissal, employee2.Dismissal);
        Assert.NotEqual(employee1.DismissalDate, employee2.DismissalDate);
    }
    /// <summary>
    /// Тест на создание и запись в Minio
    /// </summary>
    [Fact]
    public async Task GetEmployee_GenerateInMinio()
    {
        var id = 5;
        var expectedKey = $"employee-{id}.json";

        using var response = await _fixture.GatewayClient.GetAsync($"/api/CompanyEmployee?id={id}");
        response.EnsureSuccessStatusCode();

        var employee = await response.Content.ReadFromJsonAsync<CompanyEmployeeModel>(_jsonOptions);
        Assert.NotNull(employee);

        var s3Objects = await _fixture.WaitForS3ObjectAsync(expectedKey);
        Assert.NotEmpty(s3Objects);
        Assert.Single(s3Objects);

        var getObjectResponse = await _fixture.S3Client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = "companyemployee",
            Key = expectedKey
        });

        using var reader = new StreamReader(getObjectResponse.ResponseStream);
        var minioContent = await reader.ReadToEndAsync();
        var minioEmployee = JsonSerializer.Deserialize<CompanyEmployeeModel>(minioContent, _jsonOptions);

        Assert.NotNull(minioEmployee);
        Assert.Equal(employee.Id, minioEmployee.Id);
        Assert.Equal(employee.FullName, minioEmployee.FullName);
        Assert.Equal(employee.Email, minioEmployee.Email);
        Assert.Equal(employee.PhoneNumber, minioEmployee.PhoneNumber);
        Assert.Equal(employee.Salary, minioEmployee.Salary);
        Assert.Equal(employee.Dismissal, minioEmployee.Dismissal);
        Assert.Equal(employee.DismissalDate, minioEmployee.DismissalDate);
    }

    /// <summary>
    /// Тест избежания дублирования данных в Minio
    /// </summary>
    [Fact]
    public async Task GetEmployee_CheckNotDuplicateTest()
    {
        var id = 6;
        var expectedKey = $"employee-{id}.json";

        using var firstResponse = await _fixture.GatewayClient.GetAsync($"/api/CompanyEmployee?id={id}");
        firstResponse.EnsureSuccessStatusCode();

        var objectsAfterFirst = await _fixture.WaitForS3ObjectAsync(expectedKey);
        Assert.NotEmpty(objectsAfterFirst);
        Assert.Single(objectsAfterFirst);

        using var secondResponse = await _fixture.GatewayClient.GetAsync($"/api/CompanyEmployee?id={id}");
        secondResponse.EnsureSuccessStatusCode();

        await Task.Delay(TimeSpan.FromSeconds(5));

        var listResponse = await _fixture.S3Client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = "companyemployee",
            Prefix = expectedKey
        });

        Assert.Single(listResponse.S3Objects);
    }
}