using Microsoft.AspNetCore.Hosting;

using zabbixreport.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AppSettingsService>();

// Add services to the container.
builder.Services.AddRazorPages();

// Регистрируем ZabbixApiClient с нужными параметрами
builder.Services.AddSingleton(sp =>
{
    var appSettingsService = sp.GetRequiredService<AppSettingsService>();
    return new ZabbixApiClient(
        appSettingsService.Settings.ZabbixServerUrl,
        appSettingsService.Settings.ZabbixApiToken
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();
