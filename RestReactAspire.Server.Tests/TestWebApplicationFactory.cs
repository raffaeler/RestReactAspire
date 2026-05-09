using LiteDB;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using RestReactAspire.Shared.Stores;

namespace RestReactAspire.Server.Tests;

public class TestWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ILiteDatabase));
            if (dbDescriptor != null)
                services.Remove(dbDescriptor);

            LiteDbFactory.ConfigureMapper();
            services.AddSingleton<ILiteDatabase>(_ => new LiteDatabase(":memory:"));
        });
    }
}
