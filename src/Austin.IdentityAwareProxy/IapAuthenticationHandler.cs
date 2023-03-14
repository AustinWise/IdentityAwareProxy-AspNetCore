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
    public IapAuthenticationHandler(IOptionsMonitor<IapAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        IapPayload jwtPayload;

        if (Options.JwtHeader is null)
        {
            var iapFeature = Context.Features.Get<IIapFeature>();
            if (iapFeature is null)
            {
                throw new InvalidOperationException("Please make sure the call UseIap() to configure the IapMiddleWare. This should be the first thing done after calling WebApplicationBuilder.Build and UseHealthChecks.");
            }
            jwtPayload = iapFeature.Payload;
        }
        else
        {
            if (!Request.Headers.TryGetValue(Options.JwtHeader, out StringValues jwtStr))
            {
                Logger.MissingHeader(Options.JwtHeader);
                return AuthenticateResult.Fail($"Missing {Options.JwtHeader} header");
            }

            var valSettings = new SignedTokenVerificationOptions()
            {
                IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
                ExpiryClockTolerance = TimeSpan.FromMinutes(30),
                CertificatesUrl = GoogleAuthConsts.IapKeySetUrl,
                TrustedIssuers = { "https://cloud.google.com/iap" },
            };
            // TODO: support TrustedAudiences for the forwarded IAP too?

            try
            {
                jwtPayload = await JsonWebSignature.VerifySignedTokenAsync<IapPayload>(jwtStr, valSettings);
            }
            catch (InvalidJwtException ex)
            {
                Logger.InvalidJwt(ex);
                return AuthenticateResult.Fail(ex);
            }
        }


        if (jwtPayload.Subject is null || jwtPayload.Email is null)
        {
            // TODO: check AllowPublicAccess here too?
            return AuthenticateResult.NoResult();
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
}
