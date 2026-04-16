using Aspire.Hosting;
using Generator.DTO;
using System.Text.Json;

namespace ResidentialBuilding.Tests;

/// <summary>
/// Интеграционные тесты для проверки микросервисного пайплайна.
/// </summary>
/// <param name="fixture">Фикстура, чтобы не поднимать Aspire для каждого теста отдельно.</param>
public class IntegrationTests(AppFixture fixture) : IClassFixture<AppFixture>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    private static T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, _jsonOptions);
    
    private readonly DistributedApplication _app = fixture.App;
    
    /// <summary>
    /// Проверяет основной положительный сценарий:
    /// Запрос через Gateway → генерация объекта → сохранение в S3 → данные идентичны.
    /// </summary>
    [Fact]
    public async Task GetFromGateway_SavesToS3_AndDataIsIdentical()
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");
        var fileClient = _app.CreateHttpClient("residential-building-file-service", "http");
        
        var id = Random.Shared.Next(1, 10);
        
        var response = await gatewayClient.GetAsync($"/residential-building?id={id}");
        response.EnsureSuccessStatusCode();

        var apiBuilding = Deserialize<ResidentialBuildingDto>(await response.Content.ReadAsStringAsync());

        await WaitForFileInS3Async(fileClient, id, TimeSpan.FromSeconds(10));

        var s3Building = await GetBuildingFromS3Async(fileClient, id);

        Assert.NotNull(apiBuilding);
        Assert.Equal(id, apiBuilding.Id);
        Assert.NotNull(s3Building);
        Assert.Equal(id, s3Building.Id);
        Assert.Equivalent(apiBuilding, s3Building, strict: true);
    }
    
    /// <summary>
    /// Проверяет корректность сохранения объектов с разными идентификаторами.
    /// </summary>
    [Theory]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    public async Task DifferentIds_AreCorrectlySavedToS3(int id)
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");
        var fileClient = _app.CreateHttpClient("residential-building-file-service", "http");

        var response = await gatewayClient.GetAsync($"/residential-building?id={id}");
        response.EnsureSuccessStatusCode();

        var apiBuilding = Deserialize<ResidentialBuildingDto>(await response.Content.ReadAsStringAsync());

        await WaitForFileInS3Async(fileClient, id, TimeSpan.FromSeconds(10));

        var s3Building = await GetBuildingFromS3Async(fileClient, id);

        Assert.NotNull(s3Building);
        Assert.Equal(id, s3Building.Id);
        Assert.Equivalent(apiBuilding, s3Building, strict: true);
    }

    /// <summary>
    /// Проверяет работу кэширования в Generator:
    /// При повторных запросах одного и того же id возвращается идентичный объект,
    /// и S3 тоже возвращает один и тот же объект.
    /// </summary>
    [Fact]
    public async Task SameIds_GivingSameObjects()
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");
        var fileClient = _app.CreateHttpClient("residential-building-file-service", "http");
        
        var id = Random.Shared.Next(20, 30);
        
        var response = await gatewayClient.GetAsync($"/residential-building?id={id}");
        response.EnsureSuccessStatusCode();

        var apiBuilding = Deserialize<ResidentialBuildingDto>(await response.Content.ReadAsStringAsync());

        await WaitForFileInS3Async(fileClient, id, TimeSpan.FromSeconds(10));

        var s3Building = await GetBuildingFromS3Async(fileClient, id);
        
        var response1 = await gatewayClient.GetAsync($"/residential-building?id={id}");
        response.EnsureSuccessStatusCode();

        var apiBuilding1 = Deserialize<ResidentialBuildingDto>(await response1.Content.ReadAsStringAsync());

        await WaitForFileInS3Async(fileClient, id, TimeSpan.FromSeconds(10));

        var s3Building1 = await GetBuildingFromS3Async(fileClient, id);
        
        Assert.NotNull(apiBuilding);
        Assert.Equal(id, apiBuilding.Id);
        Assert.NotNull(s3Building);
        Assert.Equal(id, s3Building.Id);
        Assert.Equivalent(apiBuilding, s3Building, strict: true);
        
        Assert.NotNull(apiBuilding1);
        Assert.Equal(id, apiBuilding1.Id);
        Assert.NotNull(s3Building1);
        Assert.Equal(id, s3Building1.Id);
        Assert.Equivalent(apiBuilding1, s3Building1, strict: true);
        
        Assert.Equivalent(apiBuilding, apiBuilding1, strict: true);
        Assert.Equivalent(s3Building, s3Building1, strict: true);
    }
    
    /// <summary>
    /// Проверяет обработку нескольких разных объектов в одном тесте.
    /// Убеждается, что все сгенерированные объекты корректно сохраняются в S3.
    /// </summary>
    [Fact]
    public async Task MultipleDifferentIds_AllSavedCorrectly()
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");
        var fileClient = _app.CreateHttpClient("residential-building-file-service", "http");

        var ids = Enumerable.Range(0, 5).Select(_ => Random.Shared.Next(40, 50)).ToList();

        var apiBuildings = new List<ResidentialBuildingDto>();

        foreach (var id in ids)
        {
            var response = await gatewayClient.GetAsync($"/residential-building?id={id}");
            response.EnsureSuccessStatusCode();

            var building = Deserialize<ResidentialBuildingDto>(await response.Content.ReadAsStringAsync());
            Assert.NotNull(building);
            apiBuildings.Add(building!);

            await WaitForFileInS3Async(fileClient, id, TimeSpan.FromSeconds(8));
        }

        foreach (var id in ids)
        {
            var s3Building = await GetBuildingFromS3Async(fileClient, id);
            var original = apiBuildings.First(b => b.Id == id);

            Assert.Equal(id, s3Building.Id);
            Assert.Equivalent(original, s3Building, strict: true);
        }
    }

    /// <summary>
    /// Проверяет эндпоинт получения списка файлов в S3 и уникальность по Id.
    /// Специально используется повторяющийся id (1001), чтобы убедиться,
    /// что для одного идентификатора создаётся только один файл.
    /// </summary>
    [Fact]
    public async Task S3_ReturnsCorrectListOfFiles()
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");
        var fileClient = _app.CreateHttpClient("residential-building-file-service", "http");

        var ids = new[] { 1001, 1002, 1003, 1001 };

        foreach (var id in ids)
        {
            await gatewayClient.GetAsync($"/residential-building?id={id}");
            await WaitForFileInS3Async(fileClient, id, TimeSpan.FromSeconds(10));
        }

        var listResponse = await fileClient.GetAsync("/api/s3");
        listResponse.EnsureSuccessStatusCode();

        var fileList = Deserialize<List<string>>(await listResponse.Content.ReadAsStringAsync());

        Assert.NotNull(fileList);

        foreach (var id in ids)
        {
            Assert.Contains($"residential_building_{id}.json", fileList);
        }
        var addedFiles = fileList.Where(f => f.StartsWith("residential_building_100")).ToList();
        Assert.Equal(3, addedFiles.Count);
    }
    
    /// <summary>
    /// Проверяет обработку некорректных значений id в Gateway.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("asd")]
    public async Task InvalidId_ReturnsBadRequest(string invalidId)
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");

        var response = await gatewayClient.GetAsync($"/residential-building?id={invalidId}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Проверяет поведение при отсутствии параметра id.
    /// </summary>
    [Fact]
    public async Task MissingIdParameter_ReturnsBadRequest()
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");

        var response = await gatewayClient.GetAsync("/residential-building");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Проверяет, что при некорректном id файл в S3 НЕ создаётся.
    /// </summary>
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("asd")]
    public async Task InvalidId_DoesNotCreateFileInS3(string invalidId)
    {
        var gatewayClient = _app.CreateHttpClient("gateway", "http");
        var fileClient = _app.CreateHttpClient("residential-building-file-service", "http");

        var response = await gatewayClient.GetAsync($"/residential-building?id={invalidId}");
    
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await Task.Delay(5000);

        var fileName = $"residential_building_{invalidId}.json";
        var probe = await fileClient.GetAsync($"/api/s3/{fileName}");
    
        Assert.False(probe.IsSuccessStatusCode, "Файл не должен был появиться в S3 при невалидном id");
    }
    
    /// <summary>
    /// Ожидает появления файла в S3 с указанным id.
    /// Поллинг с таймаутом — необходим из-за асинхронной природы связи SNS и FileService.
    /// </summary>
    private static async Task WaitForFileInS3Async(HttpClient fileClient, int id, TimeSpan timeout)
    {
        var fileName = $"residential_building_{id}.json";

        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var probe = await fileClient.GetAsync($"/api/s3/{fileName}");
            if (probe.IsSuccessStatusCode)
            {
                return;
            }

            await Task.Delay(500);
        }

        throw new TimeoutException($"File {fileName} did not appear in S3 within {timeout.TotalSeconds}s");
    }

    /// <summary>
    /// Скачивает и десериализует объект ResidentialBuildingDto из S3 через FileService.
    /// </summary>
    private static async Task<ResidentialBuildingDto> GetBuildingFromS3Async(HttpClient fileClient, int id)
    {
        var fileServiceResponse = await fileClient.GetAsync($"/api/s3/residential_building_{id}.json");
        Console.WriteLine(fileServiceResponse.Content.ReadAsStringAsync());
        return Deserialize<ResidentialBuildingDto>(await fileServiceResponse.Content.ReadAsStringAsync());
    }
}