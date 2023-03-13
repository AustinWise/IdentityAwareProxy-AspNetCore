using Google.Api.Gax;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Austin.IdentityAwareProxy
{
    public class IapMiddleware
    {
        const string IAP_HEADER = "x-goog-iap-jwt-assertion";

        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public IapMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IapAuthenticationOptions> authOptions)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<IapMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress;
            var platform = await Platform.InstanceAsync();

            if (ip != null && platform.Type != PlatformType.Unknown)
            {
                // TODO: validate that the remote IP address is allowed.
            }

            if (!context.Request.Headers.TryGetValue(IAP_HEADER, out StringValues jwtStr))
            {
                _logger.MissingHeader();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // TODO: add TrustedAudiences
            var valSettings = new SignedTokenVerificationOptions()
            {
                IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
                ExpiryClockTolerance = TimeSpan.FromMinutes(30),
                CertificatesUrl = GoogleAuthConsts.IapKeySetUrl,
                TrustedIssuers = { "https://cloud.google.com/iap" }
            };
            IapPayload jwtPayload;
            try
            {
                jwtPayload = await JsonWebSignature.VerifySignedTokenAsync<IapPayload>(jwtStr, valSettings);
            }
            catch (InvalidJwtException ex)
            {
                _logger.InvalidJwt(ex);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            // TODO: check if unauthenticated users are allowed.

            context.Features.Set<IIapFeature>(new IapFeature(jwtPayload));

            await _next(context);
        }
    }
}
