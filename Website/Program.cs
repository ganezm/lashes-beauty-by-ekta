using LashesWebsite.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.Configure<GessFlowApiOptions>(builder.Configuration.GetSection("GessFlowApi"));
builder.Services.AddHttpClient<IGessFlowApiClient, GessFlowApiClient>(c => c.Timeout = TimeSpan.FromSeconds(10));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
