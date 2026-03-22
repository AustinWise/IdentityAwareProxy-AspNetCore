using Microsoft.Extensions.Options;

namespace AWise.IdentityAwareProxy;

class IapAuthenticationPostConfigureOptions : IPostConfigureOptions<IapAuthenticationOptions>
{
    public void PostConfigure(string? name, IapAuthenticationOptions options)
    {
    }
}
