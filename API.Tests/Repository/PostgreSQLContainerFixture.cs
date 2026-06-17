using Testcontainers.PostgreSql;

namespace API.Tests.Repository;

// Starts a real PostgreSQL Docker container once for all tests in a class. 
// The container is isolated — it has no data from your dev database. 
// Implements IAsyncLifetime so xUnit calls InitializeAsync before any test runs 
// and DisposeAsync after the last test completes.

public class PostgreSqlContainerFixture : IAsyncLifetime
{

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
    .WithImage("postgres:16")
    .WithDatabase("conferencetest")
    .WithUsername("testuser")
    .WithPassword("testpass")
    .Build();
    
    // Expose the connection string so the DbContext can connect to this container. 
    public string ConnectionString => _container.GetConnectionString();
    public async Task InitializeAsync() => await _container.StartAsync();
    public async Task DisposeAsync() => await _container.DisposeAsync();

    

}