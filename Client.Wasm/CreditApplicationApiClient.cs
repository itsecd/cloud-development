using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Client.Wasm;

public class CreditApplicationApiClient(HttpClient httpClient)
{
    public Task<JsonObject?> GetByIdAsync(int id)
        => httpClient.GetFromJsonAsync<JsonObject>($"api/creditapplication?id={id}");
}
