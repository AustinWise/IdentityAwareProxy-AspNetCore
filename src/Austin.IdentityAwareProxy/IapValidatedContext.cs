using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Austin.IdentityAwareProxy;

public class IapValidatedContext : ResultContext<IapAuthenticationOptions>
{
    public IapValidatedContext(HttpContext context, AuthenticationScheme scheme, IapAuthenticationOptions options)
        : base(context, scheme, options)
    {
    }
}