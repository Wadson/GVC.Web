using GVC.Web.Data;
using GVC.Web.Services;
using GVC.Web.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http.Features;
using System.Globalization;
using QuestPDF.Infrastructure;
using GVC.Web.ModelBinding;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

QuestPDF.Settings.License = LicenseType.Community;

var connectionString = builder.Configuration.GetConnectionString("ErpConnection")
    ?? throw new InvalidOperationException("A conexão 'ErpConnection' não foi configurada.");

builder.Services.AddDbContext<ErpDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContextFactory<ErpDbContext>(
    options => options.UseSqlServer(connectionString),
    ServiceLifetime.Scoped);

builder.Services.AddScoped<IPasswordHasher, Pbkdf2PasswordHasher>();

builder.Services.AddScoped<IVendaService, VendaService>();

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddScoped<IEntradaEstoqueService, EntradaEstoqueService>();

builder.Services.AddScoped<IBackupService, BackupService>();

builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = builder.Configuration.GetValue<long?>("BackupSettings:MaxUploadSizeBytes")
        ?? 5L * 1024 * 1024 * 1024);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";

        options.LogoutPath = "/Account/Logout";

        options.AccessDeniedPath = "/Account/AccessDenied";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.SlidingExpiration = true;

        options.Cookie.Name = "GVC.Auth";

        options.Cookie.HttpOnly = true;

        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization(options =>
    options.AddPolicy("Administradores", policy => policy.RequireRole("Administrador")));

builder.Services.AddRazorPages(options =>
    options.Conventions.AuthorizeFolder("/")
        .AuthorizeFolder("/Configuracoes", "Administradores").AllowAnonymousToPage("/Account/Login")
        .AllowAnonymousToPage("/Account/ForgotPassword")
        .AllowAnonymousToPage("/Account/ResetPassword")
        .AllowAnonymousToPage("/Account/AccessDenied"))
    .AddMvcOptions(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        options.ModelBinderProviders.Insert(0, new FlexibleDecimalModelBinderProvider());
    });

builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

var app = builder.Build();

await using (var initializationScope = app.Services.CreateAsyncScope())
{
    var initializationDb = initializationScope.ServiceProvider.GetRequiredService<ErpDbContext>();
    await DbInitializer.SeedPlanosContasAsync(initializationDb);
}

var ptBr = CultureInfo.GetCultureInfo("pt-BR");

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(ptBr),
    SupportedCultures = [ptBr],
    SupportedUICultures = [ptBr]
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<PermissionAuthorizationMiddleware>();

app.MapRazorPages();

app.MapControllers();

app.Run();
