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
        private readonly string[] _trustedAudiences;
        private readonly bool _allowPublicAccess;

        public IapMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IapOptions> options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<IapMiddleware>();
            _trustedAudiences = options.Value.TrustedAudiences.ToArray();
            _allowPublicAccess = options.Value.AllowPublicAccess;

            if (_trustedAudiences.Length == 0)
            {
                throw new InvalidOperationException($"You must specify at least one value for {nameof(options.Value.TrustedAudiences)}.");
            }
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
                _logger.MissingHeader(IAP_HEADER);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var valSettings = new SignedTokenVerificationOptions()
            {
                IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
                ExpiryClockTolerance = TimeSpan.FromMinutes(30),
                CertificatesUrl = GoogleAuthConsts.IapKeySetUrl,
                TrustedIssuers = { "https://cloud.google.com/iap" },
            };
            foreach (var aud in _trustedAudiences)
            {
                valSettings.TrustedAudiences.Add(aud);
            }
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

            if (!_allowPublicAccess && string.IsNullOrEmpty(jwtPayload.Subject))
            {
                _logger.UnexpectedUnauthenticatedUser();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Features.Set<IIapFeature>(new IapFeature(jwtPayload));

            await _next(context);
        }
    }
}
