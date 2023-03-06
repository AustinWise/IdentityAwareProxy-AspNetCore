using System.Security.Claims;
using System.Text.Encodings.Web;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace SandwichTracker.Services;


public class IapAuthenticationHandler : AuthenticationHandler<IapAuthenticationOptions>
{
    class IapPayload : JsonWebSignature.Payload
    {
        [JsonProperty("email")]
        public string? Email { get; set; }
    }

    const string IapAssertionHeader = "x-goog-iap-jwt-assertion";

    public IapAuthenticationHandler(IOptionsMonitor<IapAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(IapAssertionHeader, out StringValues jwtStr))
        {
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
            return AuthenticateResult.Fail(ex);
        }

        if (jwtPayload.Email is null)
        {
            return AuthenticateResult.Fail("Missing email claim.");
        }

        var validatedContext = new IapValidatedContext(Context, Scheme, Options);
        var claimsIdentity = new ClaimsIdentity(new Claim[] {
            new Claim(ClaimTypes.Actor, jwtPayload.Subject, ClaimValueTypes.String, jwtPayload.Issuer),
            new Claim(ClaimTypes.Email, jwtPayload.Email, ClaimValueTypes.Email, jwtPayload.Issuer) });
        var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);
        var properties = new AuthenticationProperties();
        var ticket = new AuthenticationTicket(claimsPrincipal, properties, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}