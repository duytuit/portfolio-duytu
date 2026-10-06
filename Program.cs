using PortfolioMvc.Services;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<SubmissionStore>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();
