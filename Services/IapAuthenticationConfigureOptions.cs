using Microsoft.Extensions.Options;

namespace SandwichTracker.Services;

class IapAuthenticationConfigureOptions : IConfigureNamedOptions<IapAuthenticationOptions>
{
    public void Configure(string? name, IapAuthenticationOptions options)
    {
        // TODO: config options
        if (string.IsNullOrEmpty(name))
        {
            return;
        }
    }

    public void Configure(IapAuthenticationOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}