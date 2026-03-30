using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AWise.IdentityAwareProxy.UnitTests;

public class IapMiddlewareTests
{
    const string TRUSTED_AUD = "/projects/12345/locations/moon-darkside1/services/sandwichtracker";
    [Fact]
    public async Task Test1()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                .UseTestServer()
                .Configure(app =>
                {
                    app.UseIap();
                })
                .ConfigureServices(services =>
                {
                    services.AddIap(o =>
                    {
                        o.TrustedAudiences.Add(TRUSTED_AUD);
                    });
                });
            }).Build();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var server = host.GetTestServer();
        var client = server.CreateClient();

        var response = await client.GetAsync("/", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
