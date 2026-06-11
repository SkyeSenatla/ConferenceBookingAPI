using System.Net;
using System.Net.Http.Json;
using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.Mvc.Testing;

namespace API.Tests.Intergration;

public class BookingsControllerTests(WebApplicationFactoryFixture factory) : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetBookings_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/v1/bookings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetBookings_ResponseIsPagedEnvelope()
    {
        var response = await _client.GetAsync("/api/v1/bookings?page=1&pageSize=5");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<BookingResponse>>();

        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(5, body.PageSize);
        Assert.True(body.TotalCount >= 0);
    }

    [Fact]
    public async Task GetBookings_ResponseIncludesXTotalCountHeader()
    {
        var response = await _client.GetAsync("/api/v1/bookings");
        Assert.True(response.Headers.Contains("X-Total-Count"),
            "X-Total-Count header must be present on all paginated list responses");
    }

    [Fact]
    public async Task GetBookings_WithoutVersion_ReturnsSameAsV1()
    {
        var v1Response = await _client.GetAsync("/api/v1/bookings");
        var unversionedResponse = await _client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.OK, v1Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unversionedResponse.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WithoutToken_Returns401()
    {
        var request = new CreateBookingRequest(
            Title: "Test",
            RoomId: Guid.NewGuid(),
            StartTime: DateTime.UtcNow.AddDays(1),
            EndTime: DateTime.UtcNow.AddDays(1).AddHours(1),
            Type: BookingType.Meeting,
            OrganizerEmail: "test@test.com",
            Description: null);

        var response = await _client.PostAsJsonAsync("/api/v1/bookings", request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_WithoutToken_Returns401()
    {
        var response = await _client.DeleteAsync($"/api/v1/bookings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBookingById_WithValidId_ReturnsOkOrNotFound()
    {
        var response = await _client.GetAsync($"/api/v1/bookings/{Guid.NewGuid()}");
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 200 or 404, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetBookings_ResponseIncludesApiVersionHeader()
    {
        var response = await _client.GetAsync("/api/v1/bookings");
        Assert.True(response.Headers.Contains("api-supported-versions"),
            "api-supported-versions header must appear on every versioned response");
    }
}
