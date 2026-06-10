using System.Net;
using System.Net.Http.Json;
using API.DTOs;
using API.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Org.BouncyCastle.Tls;

namespace API.Tests.Intergration;

public class BookingsControllerTests(WebApplicationFactoryFixture factory) : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly HttpClient _client = factory.CreateClient();


    [Fact]
    public async Task GetBookings_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("api/v1/bookings");
        //assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);


    }

    [Fact]
    public async Task GetBookings_ResponseIsPagedEnvelope()
    {
        // Act 
        var response = await _client.GetAsync("/api/v1/bookings?page=1&pageSize=5");
        var body = await response.Content.ReadFromJsonAsync<PagedResponse<BookingResponse>>();

        // Assert 
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(5, body.PageSize);
        Assert.True(body.TotalCount >= 0);
    }
    [Fact]
    public async  Task GetBookings_ResponseIncludesXTotalCountHeader()
    {
        //Act 
        var response =  await _client.GetAsync("/api/v1/bookings?page=1&pageSize=5");

        //Assert 
        Assert.True(response.Headers.Contains("X-Total-Count"), "X-Total Count header must be present on all paginated list responses"); 
    }
    [Fact]
    public async Task CreateBooking_WithoutToken_Return401()
    {
          // Arrange 
        var request = new CreateBookingRequest( 
            Title: "Test", 
            RoomId: Guid.NewGuid(), 
            StartTime: DateTime.UtcNow.AddDays(1), 
            EndTime: DateTime.UtcNow.AddDays(1).AddHours(1), 
            Type: BookingType.Meeting, 
            OrganizerEmail: "test@test.com", 
            Description: null); 
 
        // Act 
        var response = await _client.PostAsJsonAsync("/api/v1/bookings", request); 
 
        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBooking_WithoutToken_Returns401()
    {
           // Act 
        var response = await _client.DeleteAsync($"/api/v1/bookings/{Guid.NewGuid()}"); 
 
        // Assert 
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
     [Fact] 
    public async Task GetBookingById_WithValidId_ReturnsOkOrNotFound() 
    { 
        // The seed data contains bookings — a valid request must not return 500. 
        // We cannot guarantee the ID exists in CI, so we accept 200 or 404. 
        var response = await _client.GetAsync($"/api/v1/bookings/{Guid.NewGuid()}"); 
 
        Assert.True( 
            response.StatusCode == HttpStatusCode.OK || 
            response.StatusCode == HttpStatusCode.NotFound, 
            $"Expected 200 or 404, got {response.StatusCode}"); 
    } 
 
    [Fact] 
    public async Task GetBookings_ResponseIncludesVersionApiHeader() 
    { 
        // Act 
        var response = await _client.GetAsync("/api/v1/bookings"); 
 
        // Assert 
        Assert.True(response.Headers.Contains("api-supported-versions"), 
            "api-supported-versions header must appear on every versioned response"); 
    } 

}