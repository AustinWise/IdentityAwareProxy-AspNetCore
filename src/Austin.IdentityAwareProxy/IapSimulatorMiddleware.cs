using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace Austin.IdentityAwareProxy;

internal class IapSimulatorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;
    private readonly string _htmlHeader;
    private readonly string _htmlFooter;

    private IapPayload? _payload;

    public IapSimulatorMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<IapOptions> options)
    {
        this._next = next;
        this._logger = loggerFactory.CreateLogger<IapSimulatorMiddleware>();
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

        context.Features.Set(new IapSimulatorMarker());

        PathString path;
        if (!context.Request.Path.StartsWithSegments("/_iap", out path))
        {
            IapPayload? payload = _payload;
            if (payload is not null)
            {
                context.Features.Set<IIapFeature>(new IapFeature(payload));
            }
            await _next(context);
            return;
        }

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
}
