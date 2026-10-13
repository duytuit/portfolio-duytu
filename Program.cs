using PortfolioMvc.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Telegram.local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSingleton<TelegramNotifier>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();
