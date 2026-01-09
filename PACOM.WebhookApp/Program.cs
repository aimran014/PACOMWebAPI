using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using OfficeOpenXml;
using PACOM.WebhookApp.Components;
using PACOM.WebhookApp.Components.Account;
using PACOM.WebhookApp.Data;
using PACOM.WebhookApp.Model;
using PACOM.WebhookApp.Service;

// Set the EPPlus license once at startup
ExcelPackage.License.SetNonCommercialPersonal("Your Name");

var builder = WebApplication.CreateBuilder(args);

// ✅ Initialize the static DatasourcesService
DatasourcesService.Initialize(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddHttpClient();

// ✅ Register EmailConfigurationService as Singleton (for caching)
builder.Services.AddSingleton<EmailConfigurationService>();

// Configure Entity Framework and Identity
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<IdentityUserAccessor>();

builder.Services.AddScoped<IdentityRedirectManager>();

builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// ✅ Use SmtpEmailService (now reads from database)
builder.Services.AddScoped<IEmailSender<ApplicationUser>, SmtpEmailService>();

// ✅ Register DatasourcesService
builder.Services.AddScoped<DatasourcesService>();

// ✅ Register Export Services
builder.Services.AddScoped<ExcelExportService>();
builder.Services.AddScoped<PdfExportService>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
