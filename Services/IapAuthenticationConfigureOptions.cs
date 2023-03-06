using Microsoft.Extensions.Options;

namespace SandwichTracker.Services;

class IapAuthenticationConfigureOptions : IConfigureNamedOptions<IapAuthenticationOptions>
{
    public void Configure(string? name, IapAuthenticationOptions options)
    {
        // TODO: config options
    }

    public void Configure(IapAuthenticationOptions options)
    {
        // TODO: config options
    }
}