using Austin.IdentityAwareProxy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class IapServiceExtensions
{
    public static void AddIap(this IServiceCollection services)
    {
        AddIap(services, _ => { });
    }

    public static void AddIap(this IServiceCollection services, Action<IapOptions> configureOptions)
    {
        services.TryAddSingleton<IIapValidator, DefaultIapValidator>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<IapOptions>, IapConfigureOptions>());
        services.Configure(configureOptions);
        services.AddOptions<IapOptions>().ValidateDataAnnotations();
    }

    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder)
    {
        return builder.AddIap(IapDefaults.AuthenticationScheme, _ => { });
    }
    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder, string authenticationScheme)
    {
        return builder.AddIap(authenticationScheme, _ => { });
    }

    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder, string authenticationScheme, Action<IapAuthenticationOptions> configureOptions)
    {
        return builder.AddIap(authenticationScheme, displayName: null, configureOptions: configureOptions);
    }

    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<IapAuthenticationOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<IapAuthenticationOptions>, IapAuthenticationConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<IapAuthenticationOptions>, IapAuthenticationPostConfigureOptions>());
        return builder.AddScheme<IapAuthenticationOptions, IapAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
    }
}
