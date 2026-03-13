using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RestReactAspire.Server.Stores;

namespace RestReactAspire.Server.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILiteDatabase));
            if (descriptor != null)
                services.Remove(descriptor);

            LiteDbFactory.ConfigureMapper();
            services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(":memory:"));
        });
    }
}
