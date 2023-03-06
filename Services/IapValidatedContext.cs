using Microsoft.AspNetCore.Authentication;

namespace SandwichTracker.Services;

public class IapValidatedContext : ResultContext<IapAuthenticationOptions>
{
    public IapValidatedContext(HttpContext context, AuthenticationScheme scheme, IapAuthenticationOptions options)
        : base(context, scheme, options)
    {
    }
}