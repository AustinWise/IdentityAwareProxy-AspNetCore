using Microsoft.AspNetCore.Authentication;

namespace Austin.IdentityAwareProxy;

public class IapAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// See to true if you have allowed the <c>allUsers</c> access to your IAP.
    /// </summary>
    /// <remarks>
    /// If false, an error will be logged if a request from an anoymous user is received.
    /// See https://cloud.google.com/iap/docs/force-login for more details.
    /// </remarks>
    public bool AllowPublicAccess { get; set; }
}
