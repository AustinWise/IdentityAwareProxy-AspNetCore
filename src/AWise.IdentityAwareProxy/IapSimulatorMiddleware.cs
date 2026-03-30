using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace AWise.IdentityAwareProxy;

internal class IapSimulatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly string _htmlHeader;
    private readonly string _htmlFooter;
    private readonly IOptions<IapOptions> _options;

    private IapPayload? _payload;

    public IapSimulatorMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IapOptions> options)
    {
        this._next = next;
        this._logger = loggerFactory.CreateLogger<IapSimulatorMiddleware>();
        this._options = options;
        string[] splits = Properties.Resources.SimulatorIndex.Split("MAIN_CONTENT");
        _htmlHeader = splits[0];
        _htmlFooter = splits[1];
    }

    public async Task Invoke(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is not null)
        {
            if (!ip.Equals(IPAddress.Loopback) && !ip.Equals(IPAddress.IPv6Loopback))
            {
                // Only allow simulator on localhost.
                // TODO: consider crashing the process if the simulator is exposed like this?
                _logger.SimulatorExposed();
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }
        }

        IapPayload? payload = _payload;
        context.Features.Set(new IapSimulatorMarker());

        // Handle the various gcp-iap-mode query parameter values.
        var gcpIapModeValues = context.Request.Query["gcp-iap-mode"];
        if (gcpIapModeValues.Count == 1 && gcpIapModeValues[0] is string gcpIapMode)
        {
            switch (gcpIapMode.ToUpperInvariant())
            {
                case "IDENTITY":
                    context.Response.Headers.Append("x-goog-iap-generated-response", "true");
                    if (payload is null)
                    {
                        context.Response.ContentType = "text/html";
                        await context.Response.WriteAsync("IDENTITY mode is only available to authenticated users.");
                    }
                    else
                    {
                        context.Response.ContentType = "application/json";
                        await using var utfJson = new System.Text.Json.Utf8JsonWriter(context.Response.BodyWriter);
                        utfJson.WriteStartObject();
                        utfJson.WritePropertyName("email");
                        utfJson.WriteStringValue("accounts.google.com:" + payload.Email);
                        utfJson.WritePropertyName("sub");
                        utfJson.WriteStringValue(payload.Subject);
                        utfJson.WriteEndObject();
                    }
                    return;
                case "CLEAR_LOGIN_COOKIE":
                    _payload = payload = null;
                    // fall through to normal processing
                    break;
                case "SECURE_TOKEN_TEST":
                    var iapSecureTokenTestTypeValue = context.Request.Query["gcp-iap-secure-token-test-type"];
                    if (iapSecureTokenTestTypeValue.Count == 1 && iapSecureTokenTestTypeValue[0] is string iapSecureTokenTestType)
                    {
                        switch (iapSecureTokenTestType.ToUpperInvariant())
                        {
                            case "NOT_SET":
                                // fall through to normal processing
                                break;
                            case "FUTURE_ISSUE":
                            case "PAST_EXPIRATION":
                            case "ISSUER":
                            case "AUDIENCE":
                            case "SIGNATURE":
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                return;
                            default:
                                await WriteBadRequest(context);
                                return;
                        }
                    }
                    else
                    {
                        await WriteBadRequest(context);
                        return;
                    }
                    break;
                case "FORCE_LOGIN":
                    context.Response.Headers.Append("x-goog-iap-generated-response", "true");
                    context.Response.Redirect("/_iap");
                    return;
                case "DO_SESSION_REFRESH":
                case "SESSION_REFRESHER":
                    throw new NotImplementedException("Mode not yet implemented: " + gcpIapMode);
                default:
                    // Ignore invalid values and fall through to normal processing.
                    break;
            }
        }

        // Handle the normal processing path.
        PathString path;
        if (!context.Request.Path.StartsWithSegments("/_iap", out path))
        {
            if (payload is not null)
            {
                context.Features.Set<IIapFeature>(new IapFeature(payload));
            }
            else if (!_options.Value.AllowPublicAccess)
            {
                context.Response.Headers.Append("x-goog-iap-generated-response", "true");
                context.Response.ContentType = "text/html";
                var requestedWithValues = context.Request.Headers["X-Requested-With"];
                if (requestedWithValues.Count == 1 && requestedWithValues[0] is string requestedWith && requestedWith.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    await context.Response.WriteAsync("Invalid IAP credentials: empty token");
                }
                else
                {
                    context.Response.Headers.Append("x-goog-iap-generated-response", "true");
                    context.Response.Redirect("/_iap");
                }
                return;
            }
            await _next(context);
            return;
        }

        // The rest is our simulator GUI.
        if (!path.HasValue || path == "/")
        {
            await WriteIndexPage(context);
        }
        else if (path == "/login")
        {
            HandleLogin(context);
        }
        else if (path == "/logout")
        {
            _payload = null;
            context.Response.Redirect("/_iap");
        }
        else
        {
            context.Response.ContentType = "text/plain";
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            await context.Response.WriteAsync("Not Found");
        }
    }

    private async Task WriteIndexPage(HttpContext context)
    {
        HttpResponse res = context.Response;
        if (context.Request.Method != "GET")
        {
            res.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        res.StatusCode = (int)HttpStatusCode.OK;
        res.ContentType = "text/html";

        await res.WriteAsync(_htmlHeader);

        await res.WriteAsync("<h2>Status</h2>");

        IapPayload? payload = _payload;
        if (payload is null)
        {
            await res.WriteAsync("""
<p>Not logged in</p>
<p>Login:</p>
<form method="GET" action="/_iap/login">
Username: <input type="text" name="username" value="accounts.google.com:1234" size="50"/> <br/>
Email: <input type="text" name="email" value="test@example.com" size="50" /> <br/>
<input type="submit" value="Login"/>
</form>
<br/>
<form method="GET" action="/_iap/login">
<input type="submit" value="Login as anonymous"/>
</form>
""");

        }
        else
        {
            await res.WriteAsync($"""
<p>Logged in</p>
<p><em>User Name:</em> {payload.Subject}</p>
<p><em>Email:</em> {payload.Email}</p>
<p><a href="/_iap/logout">Loutout</a></p>
""");
        }

        await res.WriteAsync(_htmlFooter);
    }

    private void HandleLogin(HttpContext context)
    {
        HttpResponse res = context.Response;
        if (context.Request.Method != "GET")
        {
            res.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        string? username = context.Request.Query["username"];
        string? email = context.Request.Query["email"];

        _payload = new IapPayload()
        {
            Subject = username,
            Email = email,
        };

        res.Redirect("/_iap");
    }

    private async Task WriteBadRequest(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        context.Response.ContentType = "text/html";
        context.Response.Headers.Append("x-goog-iap-generated-response", "true");
        await context.Response.WriteAsync("There was a problem with your request.");
    }
}
