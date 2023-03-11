using Microsoft.Extensions.Options;

namespace Austin.IdentityAwareProxy;

class IapAuthenticationPostConfigureOptions : IPostConfigureOptions<IapAuthenticationOptions>
{
    public void PostConfigure(string? name, IapAuthenticationOptions options)
    {
        // TODO: config options
    }
}
