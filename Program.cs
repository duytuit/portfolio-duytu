using PortfolioMvc.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Telegram.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSingleton<TelegramNotifier>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        var hasVersion = context.Context.Request.Query.ContainsKey("v");
        var maxAge = hasVersion ? TimeSpan.FromDays(365) : TimeSpan.FromHours(1);
        context.Context.Response.Headers.CacheControl =
            $"public,max-age={(int)maxAge.TotalSeconds}{(hasVersion ? ",immutable" : string.Empty)}";
    }
});
app.UseRouting();
app.MapControllers();
app.Run();
