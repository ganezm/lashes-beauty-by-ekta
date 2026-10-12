using LashesWebsite.Services;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<GessFlowApiOptions>(builder.Configuration.GetSection("GessFlowApi"));
builder.Services.AddHttpClient<IGessFlowApiClient, GessFlowApiClient>(c => c.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        // The host's outbound network can't reach Let's Encrypt's OCSP responder to check
        // certificate revocation status, which makes .NET's default strict TLS validation fail
        // every call to the sibling flow-api.gesslabs.com domain even though the certificate
        // itself is valid. Revocation checking still happens for every other HTTPS client in
        // the app (browsers, etc.) -- this only relaxes it for this one internal API client.
        SslOptions = new SslClientAuthenticationOptions { CertificateRevocationCheckMode = X509RevocationMode.NoCheck }
    });

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

// Language: ?lang=hi|en wins and is remembered in a cookie; otherwise the cookie; otherwise English.
app.Use(async (context, next) =>
{
    var requested = context.Request.Query["lang"].ToString().ToLowerInvariant();
    string code;
    if (requested is "hi" or "en")
    {
        code = requested;
        context.Response.Cookies.Append(SiteLang.CookieName, code, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Secure = context.Request.IsHttps
        });
    }
    else
    {
        code = context.Request.Cookies[SiteLang.CookieName] == "hi" ? "hi" : "en";
    }
    context.Items[SiteLang.ItemKey] = new SiteLang(code);
    await next();
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
