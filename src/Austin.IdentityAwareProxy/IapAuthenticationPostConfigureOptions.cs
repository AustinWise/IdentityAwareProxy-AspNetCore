using Microsoft.Extensions.Options;

namespace Austin.IdentityAwareProxy;

class IapAuthenticationPostConfigureOptions : IPostConfigureOptions<IapAuthenticationOptions>
{
    public void PostConfigure(string? name, IapAuthenticationOptions options)
    {
        if (options.TrustedAudiences.Count == 0)
        {
            throw new InvalidOperationException($"You must specify at least one value for {nameof(options.TrustedAudiences)}.");
        }
    }
}
