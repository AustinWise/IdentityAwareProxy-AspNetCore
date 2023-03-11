using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Austin.IdentityAwareProxy;

public class IapAuthenticationHandler : AuthenticationHandler<IapAuthenticationOptions>
{
    const string IapAssertionHeader = "x-goog-iap-jwt-assertion";

    public IapAuthenticationHandler(IOptionsMonitor<IapAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(IapAssertionHeader, out StringValues jwtStr))
        {
            Logger.LogError($"Missing {IapAssertionHeader} header");
            return AuthenticateResult.Fail($"Missing {IapAssertionHeader} header");
        }

        var valSettings = new SignedTokenVerificationOptions()
        {
            CertificatesUrl = GoogleAuthConsts.IapKeySetUrl,
        };
        // TODO: make configurable
        valSettings.TrustedAudiences.Add("/projects/72643967898/global/backendServices/1079754107036193628");
        valSettings.TrustedIssuers.Add("https://cloud.google.com/iap");
        IapPayload jwtPayload;
        try
        {
            jwtPayload = await JsonWebSignature.VerifySignedTokenAsync<IapPayload>(jwtStr, valSettings);
        }
        catch (InvalidJwtException ex)
        {
            Logger.LogError(ex, "Failed to validate.");
            return AuthenticateResult.Fail(ex);
        }

        if (jwtPayload.Email is null)
        {
            Logger.LogError($"Missing email header");
            return AuthenticateResult.Fail("Missing email claim.");
        }

        try
        {
            var validatedContext = new IapValidatedContext(Context, Scheme, Options);
            var claims = new List<Claim> 
            {
                // TODO: confirm this is the best way to represnt the claims
                new Claim(ClaimTypes.Name, jwtPayload.Subject, ClaimValueTypes.String, jwtPayload.Issuer),
                new Claim(ClaimTypes.Email, jwtPayload.Email, ClaimValueTypes.Email, jwtPayload.Issuer),
            };
            var roles = new List<string>();
            if (jwtPayload.GoogleInfo?.AccessLevels is not null)
            {
                foreach (var level in jwtPayload.GoogleInfo.AccessLevels)
                {
                    // Role name looks like: accessPolicies/786406837856/accessLevels/level_name
                    // TODO: maybe add an option to strip the access policy prefix?
                    // Taking care to check that the policy ID matches.
                    claims.Add(new Claim(ClaimTypes.Role, level, ClaimValueTypes.String, jwtPayload.Issuer));
                    roles.Add(level);
                }
            }
            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var claimsPrincipal = new GenericPrincipal(claimsIdentity, roles.ToArray());
            var properties = new AuthenticationProperties();
            var ticket = new AuthenticationTicket(claimsPrincipal, properties, Scheme.Name);
            Logger.LogInformation("SUCCESS: created ticket");
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "failed to create identity objects");
            throw;
        }
    }

    protected override Task InitializeHandlerAsync()
    {
        return base.InitializeHandlerAsync();
    }
}
