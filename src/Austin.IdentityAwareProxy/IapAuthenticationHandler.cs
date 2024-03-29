using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Austin.IdentityAwareProxy;

public class IapAuthenticationHandler : AuthenticationHandler<IapAuthenticationOptions>
{
    private readonly IIapValidator _iapValidator;

    public IapAuthenticationHandler(IOptionsMonitor<IapAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, IIapValidator iapValidator)
        : base(options, logger, encoder)
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
                if (Context.Features.Get<IapSimulatorMarker>() is null)
                {
                    throw new InvalidOperationException("Please make sure the call UseIap() to configure the IapMiddleWare. This should be the first thing done after calling WebApplicationBuilder.Build and UseHealthChecks.");
                }
                else
                {
                    throw new InvalidOperationException("Visit /_iap to configure IAP identity.");
                }
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
                jwtPayload = await _iapValidator.Validate(jwtStr, Array.Empty<string>(), Context.RequestAborted);
            }
            catch (InvalidJwtException ex)
            {
                Logger.InvalidJwt(jwtStr, ex);
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
            var claimsIdentity = new ClaimsIdentity(Scheme.Name, ClaimTypes.NameIdentifier, ClaimTypes.Role);
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, jwtPayload.Subject, ClaimValueTypes.String, jwtPayload.Issuer, jwtPayload.Issuer, claimsIdentity));
            if (!string.IsNullOrEmpty(jwtPayload.Email))
            {
                // TODO: ASP.NET identity uses ClaimValueTypes.String for Email claims, should we?
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Email, jwtPayload.Email, ClaimValueTypes.Email, jwtPayload.Issuer, jwtPayload.Issuer, claimsIdentity));
            }
            if (Options.MapAccessPolicyToRoles.HasValue && jwtPayload.GoogleInfo?.AccessLevels is not null)
            {
                foreach (var levelStr in jwtPayload.GoogleInfo.AccessLevels)
                {
                    var level = IapAccessLevel.Parse(levelStr);
                    if (level.PolicyId == Options.MapAccessPolicyToRoles.Value)
                    {
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, level.Level, ClaimValueTypes.String, jwtPayload.Issuer, jwtPayload.Issuer, claimsIdentity));
                    }
                }
            }
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
