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
    private readonly IIapValidator _iapValidator;

    public IapAuthenticationHandler(IOptionsMonitor<IapAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, IIapValidator iapValidator)
        : base(options, logger, encoder, clock)
    {
        _iapValidator = iapValidator;
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

            try
            {
                // TODO: support TrustedAudiences for the forwarded IAP too?
                jwtPayload = await _iapValidator.Validate(jwtStr, Array.Empty<string>());
            }
            catch (InvalidJwtException ex)
            {
                Logger.InvalidJwt(ex);
                return AuthenticateResult.Fail(ex);
            }
        }


        if (jwtPayload.Subject is null)
        {
            // TODO: check AllowPublicAccess here too?
            return AuthenticateResult.NoResult();
        }

        try
        {
            var claims = new List<Claim>(2 + jwtPayload.GoogleInfo?.AccessLevels?.Count ?? 0);
            claims.Add(new Claim(ClaimTypes.NameIdentifier, jwtPayload.Subject, ClaimValueTypes.String, jwtPayload.Issuer));
            if (!string.IsNullOrEmpty(jwtPayload.Email))
            {
                claims.Add((new Claim(ClaimTypes.Email, jwtPayload.Email, ClaimValueTypes.Email, jwtPayload.Issuer)));
            }
            if (Options.MapAccessPolicyToRoles.HasValue && jwtPayload.GoogleInfo?.AccessLevels is not null)
            {
                foreach (var levelStr in jwtPayload.GoogleInfo.AccessLevels)
                {
                    var level = IapAccessLevel.Parse(levelStr);
                    if (level.PolicyId == Options.MapAccessPolicyToRoles.Value)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, level.Level, ClaimValueTypes.String, jwtPayload.Issuer));
                    }
                }
            }
            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name, ClaimTypes.NameIdentifier, ClaimTypes.Role);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
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
