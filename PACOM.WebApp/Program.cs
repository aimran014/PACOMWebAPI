using Microsoft.EntityFrameworkCore;
using PACOM.WebApp.Components;
using PACOM.WebApp.Data;
using PACOM.WebApp.Model;
using PACOM.WebApp.Service;

var builder = WebApplication.CreateBuilder(args);

// ✅ Initialize the static DatasourcesService
DatasourcesService.Initialize(builder.Configuration);
DatasourcesHelper.Initialize(builder.Configuration);


// Register DbContext
builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DatasourcesService>();


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ✅ Add this line:
builder.Services.AddHttpClient();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Bind WebhookSettings
builder.Services.Configure<WebhookSettings>(
    builder.Configuration.GetSection("WebhookSettings"));

// ✅ Register background worker
builder.Services.AddHostedService<WebhookBackgroundService>();

var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error", createScopeForErrors: true);
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


app.UseAntiforgery();
app.MapControllers(); // ✅ Make sure API endpoints are mapped
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
