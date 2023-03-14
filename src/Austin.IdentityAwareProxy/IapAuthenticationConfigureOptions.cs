using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;

namespace Austin.IdentityAwareProxy;

class IapAuthenticationConfigureOptions : IConfigureNamedOptions<IapAuthenticationOptions>
{
    private readonly IAuthenticationConfigurationProvider _authenticationConfigurationProvider;

    public IapAuthenticationConfigureOptions(IAuthenticationConfigurationProvider configurationProvider)
    {
        _authenticationConfigurationProvider = configurationProvider;
    }

    public void Configure(string? name, IapAuthenticationOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var configSection = _authenticationConfigurationProvider.GetSchemeConfiguration(name);

        if (configSection is null || !configSection.GetChildren().Any())
        {
            return;
        }

        options.JwtHeader = configSection[nameof(options.JwtHeader)] ?? options.JwtHeader;
    }

    public void Configure(IapAuthenticationOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}