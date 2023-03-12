using Austin.IdentityAwareProxy;
using Google.Cloud.Diagnostics.AspNetCore3;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    // TODO: add support for testing identity
}
else
{
    builder.Services.AddAuthentication().AddIap();
    builder.Services.AddGoogleDiagnosticsForAspNetCore();
}


// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
