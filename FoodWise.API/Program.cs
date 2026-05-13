using FoodWise.Application.Interfaces;
using FoodWise.Infrastructure.Data;
using FoodWise.Infrastructure.Identity;
using FoodWise.Infrastructure.SeedData;
using FoodWise.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// API controller servisleri eklenir.
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// MSSQL baðlantýsý yapýlýr.
builder.Services.AddDbContext<FoodWiseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Identity kullanýcý ve rol sistemi eklenir.
// Admin paneli için IdentityRole kullanýlýr.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<FoodWiseDbContext>()
    .AddDefaultTokenProviders();

// AuthService, IAuthService üzerinden API tarafýna dependency injection ile eklenir.
builder.Services.AddScoped<IAuthService, AuthService>();

// StockService, stok yönetimi iþlemleri için dependency injection container'a eklenir.
builder.Services.AddScoped<IStockService, StockService>();

// RecipeService, tarif öneri iþlemleri için dependency injection container'a eklenir.
builder.Services.AddScoped<IRecipeService, RecipeService>();

// SharingService, paylaþým ilaný ve paylaþým talebi iþlemleri için sisteme eklenir.
builder.Services.AddScoped<ISharingService, SharingService>();

// DeliveryService, QR destekli teslim kutusu iþlemleri için sisteme eklenir.
builder.Services.AddScoped<IDeliveryService, DeliveryService>();

// NotificationService, kullanýcý bildirim iþlemleri için sisteme eklenir.
builder.Services.AddScoped<INotificationService, NotificationService>();

// Karbon raporu iþlemleri için servis kaydý.
builder.Services.AddScoped<ICarbonReportService, CarbonReportService>();

// Profil bilgilerini yöneten servis burada Dependency Injection container'a eklenir.
builder.Services.AddScoped<IProfileService, ProfileService>();

// Eco puan geçmiþi ve toplam puan hesaplama iþlemleri için servis kaydý.
builder.Services.AddScoped<IEcoPointService, EcoPointService>();

// Tarif veri setini veritabanýna aktarmak için kullanýlan servis burada Dependency Injection container'a eklenir.
builder.Services.AddScoped<IRecipeDatasetImportService, RecipeDatasetImportService>();
//Admin iþlemleri için servis kaydý yapýlýr. Bu servis, admin kullanýcýlarýn yönetimi ve raporlama iþlemleri için kullanýlabilir.
builder.Services.AddScoped<IAdminService, AdminService>();
// JWT Authentication ayarlarý yapýlýr.
// Böylece [Authorize] kullanýlan endpointler token ile korunabilir.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),

        // JWT içindeki rol bilgisinin [Authorize(Roles = "Admin")] ile okunmasýný saðlar.
        RoleClaimType = ClaimTypes.Role
    };
});

// Rol bazlý yetkilendirme için authorization servisi eklenir.
builder.Services.AddAuthorization();

var app = builder.Build();

// Uygulama ilk açýldýðýnda migration ve baþlangýç seed verileri çalýþtýrýlýr.
// Admin/User rolleri ve admin kullanýcý bu aþamada oluþturulur.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FoodWiseDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await FoodWiseDbSeeder.SeedAsync(context, userManager, roleManager);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();