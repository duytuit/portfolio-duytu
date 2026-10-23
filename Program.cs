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
app.Use(async (context, next) => {
    if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method)) {
        var path = context.Request.Path.Value ?? "/";
        if (!Path.HasExtension(path)) {
            var normalized = path == "/" ? "/" : path.TrimEnd('/').ToLowerInvariant();
            if (path != normalized) { context.Response.Redirect(normalized + context.Request.QueryString, permanent: true); return; }
        }
    }
    await next();
    if (context.Response.StatusCode >= 400) context.Response.Headers["X-Robots-Tag"] = "noindex";
});
app.UseRouting();
app.MapControllers();
app.Run();
