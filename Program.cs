using PortfolioMvc.Services;
using PortfolioMvc.Modules.VideoTrimmer;
using PortfolioMvc.Modules.TikTok;
using Serilog;

var logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        path: Path.Combine(logDirectory, "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting web host. File logs directory: {LogDirectory}", logDirectory);
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog();
    builder.Configuration.AddJsonFile("appsettings.Telegram.local.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
    builder.Services.AddControllersWithViews();
    builder.Services.AddSingleton<SubmissionStore>();
    builder.Services.AddSingleton<TelegramNotifier>();
    builder.Services.AddVideoTrimmerModule(builder.Configuration);
    builder.Services.AddTikTokModule(builder.Configuration);
    var app = builder.Build();
    app.Lifetime.ApplicationStarted.Register(() =>
        Log.Information("Web host started. File logs directory: {LogDirectory}", logDirectory));
    if (app.Environment.IsDevelopment())
    {
        // Show the full exception, stack trace, and request details while debugging locally.
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Keep exception details out of production responses; Serilog records the exception.
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }
    app.UseSerilogRequestLogging();
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
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
