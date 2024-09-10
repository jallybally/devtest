using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using zabbixreport.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<AppSettingsService>();
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDBSettings"));
//builder.Services.AddSingleton<AppSettingsService>();
builder.Services.AddSingleton<NomenclatureService>();

// Add services to the container.
builder.Services.AddRazorPages();

//ZabbixApiClient
builder.Services.AddSingleton(sp =>
{
    var appSettingsService = sp.GetRequiredService<AppSettingsService>();
    return new ZabbixApiClient(
        appSettingsService.Settings.ZabbixServerUrl,
        appSettingsService.Settings.ZabbixApiToken
    );
});

builder.Services.AddSingleton<IMongoClient, MongoClient>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<MongoDBSettings>>().Value;
    return new MongoClient(settings.ConnectionString);
});

builder.Services.AddScoped<MongoDBService>();

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
