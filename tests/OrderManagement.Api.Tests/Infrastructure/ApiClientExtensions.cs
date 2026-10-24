using System.Net.Http.Json;
using OrderManagement.Api.Features.Boards;

namespace OrderManagement.Api.Tests.Infrastructure;

/// <summary>Creates test data through the public API, the same way a client would.</summary>
public static class ApiClientExtensions
{
    public static async Task<BoardResponse> CreateBoardAsync(this HttpClient client, string name, params int[] componentIds)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            BoardEndpoints.Route, new BoardRequest(name, null, 100m, 80m, [.. componentIds]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BoardResponse>())!;
    }
}
