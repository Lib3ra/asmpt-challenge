using System.Net.Http.Json;
using OrderManagement.Api.Features.Boards;
using OrderManagement.Api.Features.Orders;

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

    public static async Task<OrderResponse> CreateOrderAsync(this HttpClient client, string name, params int[] boardIds)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            OrderEndpoints.Route, new OrderRequest(name, null, new DateOnly(2026, 10, 24), [.. boardIds]));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }
}
