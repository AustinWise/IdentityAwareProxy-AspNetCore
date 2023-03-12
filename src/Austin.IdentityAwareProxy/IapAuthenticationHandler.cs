using System.Diagnostics;
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
            Logger.MissingHeader();
            return AuthenticateResult.Fail($"Missing {IapAssertionHeader} header");
        }

        var valSettings = new SignedTokenVerificationOptions()
        {
            IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
            ExpiryClockTolerance = TimeSpan.FromMinutes(30),
            CertificatesUrl = GoogleAuthConsts.IapKeySetUrl,
        };
        Debug.Assert(Options.TrustedAudiences.Count != 0);
        foreach (var aud in Options.TrustedAudiences)
        {
            valSettings.TrustedAudiences.Add(aud);
        }
        valSettings.TrustedIssuers.Add("https://cloud.google.com/iap");

        IapPayload jwtPayload;
        try
        {
            jwtPayload = await JsonWebSignature.VerifySignedTokenAsync<IapPayload>(jwtStr, valSettings);
        }
        catch (InvalidJwtException ex)
        {
            Logger.InvalidJwt(ex);
            return AuthenticateResult.Fail(ex);
        }

        if (jwtPayload.Subject is null || jwtPayload.Email is null)
        {
            if (Options.AllowPublicAccess)
            {
                return AuthenticateResult.NoResult();
            }
            else
            {
                const string ERROR_MESSAGE = $"User identity missing in JWT. Set option {nameof(Options.AllowPublicAccess)} to true if you meant to allow unathenticated users to access this site.";
                Logger.UnexpectedUnauthenticatedUser();
                return AuthenticateResult.Fail(ERROR_MESSAGE);
            }
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
            Logger.SuccessfullyCreatedPrincipal();
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.FailureCreatingPrincipal(ex);
            throw;
        }
    }

    protected override Task InitializeHandlerAsync()
    {
        return base.InitializeHandlerAsync();
    }
}
