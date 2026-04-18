using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace Client.Wasm;

public class VehicleApiClient(HttpClient httpClient)
{
    public async Task<JsonObject?> GetVehicleAsync(int id)
    {
        return await httpClient.GetFromJsonAsync<JsonObject>($"api/vehicle/{id}");
    }
}
