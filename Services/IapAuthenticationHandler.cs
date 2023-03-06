using System.Security.Claims;
using System.Security.Principal;
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

    private AuthenticateResult createMockResult()
    {
        var validatedContext = new IapValidatedContext(Context, Scheme, Options);
        var claims = new Claim[] 
        {
            new Claim(ClaimTypes.Name, "accounts.google.com:1234", ClaimValueTypes.String, "https://cloud.google.com/iap"),
            new Claim(ClaimTypes.Email, "test@awise.us", ClaimValueTypes.Email, "https://cloud.google.com/iap"),
        };
        var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
        var claimsPrincipal = new GenericPrincipal(claimsIdentity, null);
        var properties = new AuthenticationProperties();
        var ticket = new AuthenticationTicket(claimsPrincipal, properties, Scheme.Name);
        Logger.LogError("scheme: " + Scheme.Name);
        return AuthenticateResult.Success(ticket);
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
            var claims = new Claim[] 
            {
                new Claim(ClaimTypes.Name, jwtPayload.Subject, ClaimValueTypes.String, jwtPayload.Issuer),
                new Claim(ClaimTypes.Email, jwtPayload.Email, ClaimValueTypes.Email, jwtPayload.Issuer),
            };
            var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
            var claimsPrincipal = new GenericPrincipal(claimsIdentity, null);
            var properties = new AuthenticationProperties();
            var ticket = new AuthenticationTicket(claimsPrincipal, properties, Scheme.Name);
            Logger.LogError("SUUCCESS: created ticket");
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "failed to create idenity objects");
            throw;
        }
    }
}