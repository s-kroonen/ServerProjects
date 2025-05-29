using HemelBingo.Components;
using HemelBingo.Data;
using HemelBingo.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var sqlConnectionString = builder.Configuration.GetValue<string>("SqlConnectionStringLocal");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

builder.Services.AddScoped<BingoSessionService>();
builder.Services.AddScoped<UserSessionService>();

builder.Services.AddDistributedMemoryCache(); // For session, optional
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Needed for accessing HttpContext in Razor components
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

//builder.Services.AddAuthentication("HemelAuth")
//    .AddCookie("HemelAuth", options =>
//    {
//        options.LoginPath = "/login";
//        options.ExpireTimeSpan = TimeSpan.FromHours(2); // Auto-logout time
//        options.SlidingExpiration = true;               // Extend on activity
//    });

builder.Services.AddAuthorization();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

//app.UseAuthentication();
//app.UseAuthorization();
 app.UseSession(); // Uncomment if you use it explicitly

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
