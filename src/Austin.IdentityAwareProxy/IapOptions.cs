using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;

namespace Austin.IdentityAwareProxy;

public class IapOptions
{
    /// <summary>
    /// See to true if you have allowed the <c>allUsers</c> access to your IAP.
    /// </summary>
    /// <remarks>
    /// If false, an error will be logged if a request from an anoymous user is received.
    /// See https://cloud.google.com/iap/docs/force-login for more details.
    /// </remarks>
    public bool AllowPublicAccess { get; set; }

    /// <summary>
    /// Which IAP instances are allowed to access this application. At least one is required.
    /// </summary>
    /// <remarks>
    /// Should be in the form <c>/projects/PROJECT_NUMBER/apps/PROJECT_ID</c> or <c>/projects/PROJECT_NUMBER/global/backendServices/SERVICE_ID</c>.
    /// See https://cloud.google.com/iap/docs/signed-headers-howto#iap_validate_jwt-ruby for more information about how to look up these <c>aud</c> values.
    /// </remarks>
    [MinLength(1)]
    public IList<string> TrustedAudiences { get; } = new List<string>();
}
