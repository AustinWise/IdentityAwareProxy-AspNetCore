using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace Austin.IdentityAwareProxy;

class IapConfigureOptions : IConfigureOptions<IapOptions>
{
    private readonly IConfiguration _config;

    public IapConfigureOptions(IConfiguration configuration)
    {
        _config = configuration;
    }

    public void Configure(IapOptions options)
    {
        var configSection = _config.GetSection("IdentityAwareProxy");

        if (configSection is null || !configSection.GetChildren().Any())
        {
            return;
        }

        var audience = configSection["TrustedAudience"];
        var audiences = configSection.GetSection(nameof(IapOptions.TrustedAudiences)).GetChildren().Select(aud => aud.Value).ToList();
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
}