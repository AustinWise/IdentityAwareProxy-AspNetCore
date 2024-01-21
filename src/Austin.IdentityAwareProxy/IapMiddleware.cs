using System.Diagnostics;
using System.Net;
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

        // Where connections should be coming from for load balancers.
        // Documented in these locations:
        // * https://cloud.google.com/iap/docs/load-balancer-howto#firewall
        // * https://cloud.google.com/load-balancing/docs/https
        static readonly IPNetwork s_gfeNet1 = new IPNetwork(IPAddress.Parse("35.191.0.0"), 16);
        static readonly IPNetwork s_gfeNet2 = new IPNetwork(IPAddress.Parse("130.211.0.0"), 22);

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

            // This should be enforced by the options validator.
            Debug.Assert(_trustedAudiences.Length != 0);
        }

        public async Task Invoke(HttpContext context)
        {
            var ip = context.Connection.RemoteIpAddress;

            if (ip != null)
            {
                var platform = await Platform.InstanceAsync();
                if (platform.Type == PlatformType.Gce || platform.Type == PlatformType.Gke)
                {
                    if (!s_gfeNet1.Contains(ip) && !s_gfeNet2.Contains(ip))
                    {
                        _logger.UnexpectedIpAddress(ip);
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }
                }
                else if (platform.Type == PlatformType.CloudRun)
                {
                    // The requests appear to always come from the same link-local IP address, so we can't
                    // tell where its coming from. However it appears the Cloud Run is stripping the
                    // IAP header when it is sent from someone other than IAP. So perhaps we don't
                    // have to worry about a pass-the-JWT attack on Cloud Run.
                }
                else if (platform.Type == PlatformType.Gae)
                {
                    // TODO: try app engine
                }
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
                jwtPayload = await _iapValidator.Validate(jwtStr, _trustedAudiences, context.RequestAborted);
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
