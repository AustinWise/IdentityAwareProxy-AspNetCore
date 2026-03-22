
# Google Cloud Identity Aware Proxy authentication handler for ASP.NET Core

This is a work in progress and not an official Google project.
This library is for some personal projects I'm working on.
The goal is to create an ASP.NET Core authentication provider
for [Google Cloud Identity Aware Proxy](https://cloud.google.com/iap).

Currently implemented features:

* Sets the HttpContext.User to a principal that:
  * Uses the subject claim of the IAP JWT as a user name (it looks like "accounts.google.com:1234", where 1234 is the user's ID)
  * An email claim containing the user's email address.
  * Access levels are set as the roles for the user.
* A simulator GUI for simulating IAP when testing locally.
* Blocks all requests that have a missing or invalid IAP JWT.
* On GCE and GKE, blocks all requests from IP addresses other the Google Cloud Load Balancer IP range.
  On Cloud Run inauthentic JWT headers are stripped before we see them, so we don't have to worry
  about IP checking.

## Usage

Add reference to the [Nuget package](https://www.nuget.org/packages/AWise.IdentityAwareProxy).

In your program, add the IAP services and authentication to the `WebApplicationBuilder`:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddIap();
builder.Services.AddAuthentication().AddIap();
```

Once you create the `WebApplication`, call `UseIap()` to insert the

```csharp
var app = builder.Build();

// Configure the HTTP request pipeline.
// The health check need to come before IAP because health checks don't have the IAP header.
// And the IAP middleware will block requests without the IAP header.
app.UseHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
    // Simulates IAP by injecting a fake user. This can be configured at /_iap .
    // It will block any request that does not come from local host in an attempt to prevent you
    // from shipping the simulator in production.
    app.UseIapSimulator();
}
else
{
    app.UseIap();

    // UseForwardedHeaders must be after UseIap for the IP checking in in UseIap to work correctly.
    // UseForwardedHeaders is needed so that UseHsts knows we are actually using HTTPS and will send the header.
    var forwardOpts = new ForwardedHeadersOptions()
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        // As documented here, second from the end is the actual client IP address: https://cloud.google.com/load-balancing/docs/https#x-forwarded-for_header
        ForwardLimit = 2,
    };
    // The IAP middleware already validated the IP address of the upstream and the IAP JWT token.
    // So remove the restriction that only localhost can forward.
    forwardOpts.KnownIPNetworks.Clear();
    forwardOpts.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardOpts);

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Further handlers...
```

In your `appsettings.json`, setup the authentication and add the JWT audience code. You can find the
JWT audience code in the [Google Cloud Console](https://console.cloud.google.com/security/iap). You
can use more than one code if your application is published behind multiple IAP instances.
You can set `AllowPublicAccess` to `true` if you are using the
[public access feature](https://docs.cloud.google.com/iap/docs/managing-access#public_access).

```json
{
    "Authentication": {
        "DefaultScheme": "IAP",
        "Schemes": {
            "IAP": {
            }
        }
    },
    "IdentityAwareProxy": {
        "AllowPublicAccess": false,
        "TrustedAudiences": [
            "/projects/72643967898/global/backendServices/1079754107036193628"
        ]
    }
}
```

## TODO

* Automated testing.
* Actually implement something interesting in the example app.
* Consider integrating with
  [ASP.NET Identity](https://learn.microsoft.com/en-us/aspnet/identity/overview/getting-started/introduction-to-aspnet-identity).
  This might make it easier for the user identity to be part of a larger Entity Framework database schema.
  This might be challenging to make smooth, as ASP.NET Identity treats its cookie as the source of user identity
  and this is not currently customizable.
* Add support [external identities](https://cloud.google.com/iap/docs/enable-external-identities).
* Add the option to validate the audience of the JWT in the alternate header.
* Add options for customizing how the username is chosen.
  * User id, with the `accounts.google.com:` prefix. (current default)
  * Email
  * A custom delegate on the events object.
  * See also [these docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/additional-claims?view=aspnetcore-7.0)
    for inspiration.
* Add options for customizing how IAP access levels are translated into roles.
  * Disable translating to roles
  * Strip prefix
  * Custom delegate on the event object for transforming.
* Add NativeAOT / Trim compatibility. This might require using a different library to validate the JWT.
  [Microsoft.AspNetCore.Authentication.JwtBearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer)
  for example is trimmable.
* Test on App Engine, including checking what IP address the requests come from and filtering out
  bad IP address in `IapMiddleware`.
* Make the simulator GUI more attractive. It should probably also leverage Razor rather than string
  concatenation.
* Consider whether there it is possible to create a common abstraction for IAP and similar services.
  Similar services include:
  * [Cloudflare Access](https://developers.cloudflare.com/learning-paths/zero-trust-web-access/migrate-applications/consume-jwt/)
  * [AWS Verified Access](https://docs.aws.amazon.com/verified-access/latest/ug/user-claims-passing.html)
  * [Tailscale Serve](https://tailscale.com/docs/features/tailscale-serve#identity-headers)
  * [Microsoft Entra application proxy](https://learn.microsoft.com/en-us/entra/identity/app-proxy/application-proxy-configure-single-sign-on-with-headers) -
    they don't really appear to have an IAP equivalent. This is the closest, which uses unsigned-headers
    and is targeted more at on-premise apps.
