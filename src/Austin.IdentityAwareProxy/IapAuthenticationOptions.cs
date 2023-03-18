using Microsoft.AspNetCore.Authentication;

namespace Austin.IdentityAwareProxy;

public class IapAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Which header to read the JWT from. If not set, the JWT is ready from the default header.
    /// </summary>
    /// <remarks>
    /// This is useful when you have a backend service that is behind IAP. The caller of the service
    /// can [use a service account to authenticate with the IAP](https://cloud.google.com/iap/docs/authentication-howto#authenticating_from_a_service_account)
    /// . They can also pass the IAP JWT they received in a different header. This backend service can
    /// then pull the JWT from this other header to get the original identity of the user who called
    /// the frontend service.
    /// </remarks>
    public string? JwtHeader { get; set; }

    public long? MapAccessPolicyToRoles { get; set; }
}
