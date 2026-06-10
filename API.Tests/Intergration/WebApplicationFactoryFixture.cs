using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection; 

namespace API.Tests.Intergration; 

// Starts the application once for all tests in a class, 
//IClassFixture<T>  is the xUnit mechanism for shared, expensive setup

public class WebApplicationFactoryFixture: WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            //Intergration tests run against a real database by default
            /*
            Override specific services here if needed — e.g. swap the 
             connection string for a TestContainers database.
            */
        });
    }
}