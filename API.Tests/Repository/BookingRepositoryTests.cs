using API.Data;
using API.Models;
using API.Repositories; 

using Microsoft.EntityFrameworkCore;

namespace API.Tests.Repository;

// IClassFixture<T> shares the container across all tests in this class. 
// The container starts once, all tests run, container is destroyed. 

public class BookingRepositoryTests(PostgreSqlContainerFixture  fixture) : IClassFixture<PostgreSqlContainerFixture>
{
    
    private BookingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>().UseNpgsql(fixture.ConnectionString).Options; 

        var context = new BookingDbContext(options); 

        //Apply all migrations - Create a schema exactly as production does
        //The check contraints, indexes, and computed columns are created here 

        context.Database.Migrate();

        return context; 
    }

    [Fact]
    public async Task GetAllAsync_Page1_ReturnsFirstPageOfResults()
    {
        //arrange
       await using var context = CreateContext();
       await SeedData.SeedAsync(context); 
       var repository = new BookingRepository(context); 

       //Act

       var result = await repository.GetAllAsync(page: 1, pageSize: 2); 
 
        // Assert 
        Assert.Equal(1, result.Page); 
        Assert.Equal(2, result.PageSize); 
        Assert.Equal(2, result.Data.Count()); 
        Assert.True(result.TotalCount >= 2); 
        Assert.True(result.HasNextPage); 
        Assert.False(result.HasPreviousPage);
    }
    [Fact]

    public async Task GetAllAsync_Page2_ReturnsDifferentResults() 
    { 
        // Arrange 
        await using var context = CreateContext(); 
        await SeedData.SeedAsync(context); 
        var repository = new BookingRepository(context); 
 
        // Act 
        var page1 = await repository.GetAllAsync(page: 1, pageSize: 2); 
        var page2 = await repository.GetAllAsync(page: 2, pageSize: 2); 
 
        // Assert — pages must not overlap 
        var page1Ids = page1.Data.Select(b => b.Id).ToHashSet(); 
        var page2Ids = page2.Data.Select(b => b.Id).ToHashSet(); 
 
        Assert.Empty(page1Ids.Intersect(page2Ids)); 
        Assert.True(page2.HasPreviousPage); 
    }

    /*

    Homework  

     public async Task GetAllAsync_ResultsAreOrderedByStartTimeAscending() 

      public async Task CheckConstraint_RejectsBookingWithEndTimeBeforeStartTime()

       private async Task<Room> SeedRoom(BookingDbContext context)
    */

}

