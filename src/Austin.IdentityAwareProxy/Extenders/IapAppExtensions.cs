using Austin.IdentityAwareProxy;

namespace Microsoft.AspNetCore.Builder;

public static class IapAppExtensions
{
    public static IApplicationBuilder UseIap(this IApplicationBuilder app)
    {
        app.UseMiddleware<IapMiddleware>();
        return app;
    }

    public static IApplicationBuilder UseIapSimulator(this IApplicationBuilder app)
    {
        app.UseMiddleware<IapSimulatorMiddleware>();
        return app;
    }
}
