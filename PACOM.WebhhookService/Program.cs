using Microsoft.EntityFrameworkCore;
using PACOM.WebhhookService;
using PACOM.WebhookApp.Data;
using PACOM.WebhookApp.Model;
using PACOM.WebhookApp.Service;
using System.Runtime.InteropServices;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

// ✅ Initialize the static DatasourcesService
DatasourcesService.Initialize(builder.Configuration);

// Register DbContext
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DatasourcesService>();


// ✅ Add this line:
builder.Services.AddHttpClient();

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Add Event Viewer logging ONLY on Windows
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.Logging.AddEventLog();
}

// Bind WebhookSettings
builder.Services.Configure<WebhookSettings>(
    builder.Configuration.GetSection("WorkerSettings"));

var host = builder.Build();
host.Run();
