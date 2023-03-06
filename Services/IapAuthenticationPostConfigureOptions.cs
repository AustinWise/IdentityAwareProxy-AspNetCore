using Microsoft.Extensions.Options;

namespace SandwichTracker.Services;

class IapAuthenticationPostConfigureOptions : IPostConfigureOptions<IapAuthenticationOptions>
{
    public void PostConfigure(string? name, IapAuthenticationOptions options)
    {
        // TODO: config options
    }
}
