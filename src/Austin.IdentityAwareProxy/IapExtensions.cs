using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Austin.IdentityAwareProxy;

namespace Microsoft.Extensions.DependencyInjection;

public static class IapExtensions
{
    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder)
    {
        return AddIap(builder, IapDefaults.AuthenticationScheme, _ => { });
    }
    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder, string authenticationScheme)
    {
        return AddIap(builder, authenticationScheme, _ => { });
    }

    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder, string authenticationScheme, Action<IapAuthenticationOptions> configureOptions)
    {
        return AddIap(builder, authenticationScheme, displayName: null, configureOptions: configureOptions);
    }

    public static AuthenticationBuilder AddIap(this AuthenticationBuilder builder, string authenticationScheme, string? displayName, Action<IapAuthenticationOptions> configureOptions)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<IapAuthenticationOptions>, IapAuthenticationConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<IapAuthenticationOptions>, IapAuthenticationPostConfigureOptions>());
        return builder.AddScheme<IapAuthenticationOptions, IapAuthenticationHandler>(authenticationScheme, displayName, configureOptions);
    }
}
