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
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}
// A server error shows the site's own page (with the menu), not a blank screen. A missing page is handled by the fallback route below.

// Headers every page carries. A staging copy of the site (Site:NoIndex) also tells search engines to stay away.
var siteNoIndex = builder.Configuration.GetValue<bool>("Site:NoIndex");
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "SAMEORIGIN";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
    if (siteNoIndex) headers["X-Robots-Tag"] = "noindex, nofollow";
    await next();
});
app.UseRouting();

// Hindi is switched off until Site:HindiEnabled is true; everyone gets English.
var hindiEnabled = builder.Configuration.GetValue<bool>("Site:HindiEnabled");
// Language: ?lang=hi|en wins and is remembered in a cookie; otherwise the cookie; otherwise English.
app.Use(async (context, next) =>
{
    var requested = context.Request.Query["lang"].ToString().ToLowerInvariant();
    string code;
    if (!hindiEnabled) code = "en";
    else if (requested is "hi" or "en")
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

// What search engines are told about the site, and the list of pages to look at.
app.MapGet("/robots.txt", (HttpContext context) =>
{
    var origin = $"{context.Request.Scheme}://{context.Request.Host}";
    var text = siteNoIndex
        ? "User-agent: *\nDisallow: /\n"
        : $"User-agent: *\nAllow: /\nDisallow: /gift-cards/thank-you\nDisallow: /error/\n\nSitemap: {origin}/sitemap.xml\n";
    return Results.Text(text, "text/plain; charset=utf-8");
});
app.MapGet("/sitemap.xml", (HttpContext context) =>
{
    var origin = $"{context.Request.Scheme}://{context.Request.Host}";
    var pages = new (string Path, string Priority)[] { ("/", "1.0"), ("/services", "0.9"), ("/book", "0.9"), ("/lookbook", "0.7"), ("/academy", "0.7"), ("/about", "0.6"), ("/contact", "0.6"), ("/gift-cards", "0.5") };
    var xml = new System.Text.StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
    if (!siteNoIndex)
        foreach (var (path, priority) in pages) xml.Append($"  <url><loc>{origin}{path}</loc><priority>{priority}</priority></url>\n");
    xml.Append("</urlset>\n");
    return Results.Text(xml.ToString(), "application/xml; charset=utf-8");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Anything that matched nothing gets the site's own "page not found" page, with a real 404 status.
app.MapFallbackToController("Index", "NotFoundPage");

app.Run();
