using Google.Api.Gax;
using Google.Apis.Auth;
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
        private readonly IIapValidator _iapValidator;
        private readonly string[] _trustedAudiences;
        private readonly bool _allowPublicAccess;

        public IapMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IapOptions> options, IIapValidator iapValidator)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<IapMiddleware>();
            _trustedAudiences = options.Value.TrustedAudiences.ToArray();
            _allowPublicAccess = options.Value.AllowPublicAccess;
            _iapValidator = iapValidator;

            // if (_trustedAudiences.Length == 0)
            // {
            //     throw new InvalidOperationException($"You must specify at least one value for {nameof(options.Value.TrustedAudiences)}.");
            // }
            System.Console.WriteLine($"len: {_trustedAudiences.Length}");
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

            IapPayload jwtPayload;
            try
            {
                jwtPayload = await _iapValidator.Validate(jwtStr, _trustedAudiences);
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
