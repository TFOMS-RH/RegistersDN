using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RegistrDN.Data;
using RegistrDN.Data.Repositories;
using RegistrDN.Models.Entities;
using RegistrDN.Models.DTOs.Import;
using RegistrDN.Models.DTOs.Export;
using RegistrDN.Services.Interfaces;
using RegistrDN.Services.Xml;
using RegistrDN.Services.Zip;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// ============================================
// Identity
// ============================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Настройка Identity
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// ============================================
// Регистрация репозиториев
// ============================================
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Регистрация AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// ============================================
// Регистрация XML сервисов
// ============================================
builder.Services.AddScoped<IXmlService<GstImportDto, GstExportDto, GstEntity>, GstXmlService>();
builder.Services.AddScoped<IXmlService<GptImportDto, GptExportDto, GptEntity>, GptXmlService>();
builder.Services.AddScoped<IXmlService<GfImportDto, GfExportDto, GfEntity>, GfXmlService>();
builder.Services.AddScoped<IXmlService<GsmImportDto, GsmExportDto, GstEntity>, GsmXmlService>();
builder.Services.AddScoped<IXmlService<GpmImportDto, GpmExportDto, GptEntity>, GpmXmlService>();

// Регистрация Zip сервиса
builder.Services.AddScoped<ZipValidationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ============================================
// Authentication & Authorization
// ============================================
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ============================================
// ИНИЦИАЛИЗАЦИЯ РОЛЕЙ ПРИ ЗАПУСКЕ
// ============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await InitializeRolesAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ошибка инициализации ролей");
    }
}

app.Run();

// ============================================
// МЕТОД ИНИЦИАЛИЗАЦИИ РОЛЕЙ
// ============================================
static async Task InitializeRolesAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roleNames = { "Admin", "TFOMS", "MO" };

    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Создаем администратора, если его нет
    var adminEmail = "admin@registrdn.ru";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Администратор системы",
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
            Console.WriteLine("✅ Администратор создан: admin@registrdn.ru / Admin123!");
        }
    }
}