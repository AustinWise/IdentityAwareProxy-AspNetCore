using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

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

        var audience = configSection["TrustedAudience"];
        var audiences = configSection.GetSection(nameof(IapAuthenticationOptions.TrustedAudiences)).GetChildren().Select(aud => aud.Value).ToList();
        if (audience is not null)
        {
            audiences.Add(audience);
        }
        foreach(var aud in audiences)
        {
            options.TrustedAudiences.Add(aud!);
        }

        options.AllowPublicAccess = TryGetBool(configSection, nameof(options.AllowPublicAccess), options.AllowPublicAccess);
    }

    private static bool TryGetBool(IConfiguration config, string key, bool defaultValue)
    {
        string? value = config[key];
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        else
        {
            return bool.Parse(value);
        }
    }

    public void Configure(IapAuthenticationOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}