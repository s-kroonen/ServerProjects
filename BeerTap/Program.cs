using BeerTap.Components;
using BeerTap.Repositories;
using BeerTap.Services;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

var builder = WebApplication.CreateBuilder(args);
var sqlConnectionString = builder.Configuration.GetValue<string>("SqlConnectionString");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<TapQueueManager>();
builder.Services.AddSingleton<MqttService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());

builder.Services.AddTransient<UserRepository>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<UserRepository>>(); // Get logger from DI container
    return new UserRepository(sqlConnectionString, logger); // Inject the logger into the repository
});
//builder.Services.AddTransient<UserRepository, UserRepository>(o => new UserRepository(sqlConnectionString));
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<UserService>();



builder.Services.AddControllers();




var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapControllers();

app.Run();
