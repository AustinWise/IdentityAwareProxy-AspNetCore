using Google.Cloud.Diagnostics.Common;

var builder = WebApplication.CreateBuilder(args);

// TODO: add support for testing identity
if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication().AddIap();
    builder.Logging.AddGoogle();
}

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

var defaultRoute = app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

if (!app.Environment.IsDevelopment())
{
    defaultRoute.RequireAuthorization();
}

// For running on Google Cloud Run
var portStr = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(portStr))
{
    int port = int.Parse(portStr, System.Globalization.CultureInfo.InvariantCulture);
    app.Run($"http://0.0.0.0:{port}");
}
else
{
    app.Run();
}
