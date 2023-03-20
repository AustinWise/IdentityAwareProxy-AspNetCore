using Google.Cloud.Diagnostics.AspNetCore3;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // TODO: add support for testing identity
}
else
{
    builder.Services.AddIap();
    builder.Services.AddAuthentication().AddIap();
    builder.Services.AddGoogleDiagnosticsForAspNetCore("72643967898");
}


// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
// The health check should be the only thing that is processed before the IAP.
app.UseHealthChecks("/health");

if (app.Environment.IsDevelopment())
{
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
    forwardOpts.KnownNetworks.Clear();
    forwardOpts.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardOpts);

    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// For running on Google Cloud Run
var portStr = Environment.GetEnvironmentVariable("PORT");

if (string.IsNullOrEmpty(portStr))
{
    app.UseHttpsRedirection();
}
else
{
    // If we are running on Cloud Run, we don't do HTTPs.
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

var defaultRoute = app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Environment.IsDevelopment())
{
    //defaultRoute.RequireAuthorization();
}

if (string.IsNullOrEmpty(portStr))
{
    app.Run();
}
else
{
    int port = int.Parse(portStr, System.Globalization.CultureInfo.InvariantCulture);
    app.Run($"http://0.0.0.0:{port}");
}
