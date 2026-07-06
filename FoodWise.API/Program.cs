
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

// API controller yapýsý projeye eklenir.
builder.Services.AddControllers();

// Swagger/OpenAPI servisleri eklenir.
// Geliþtirme ortamýnda endpointlerin test edilmesini kolaylaþtýrýr.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// SQL Server veritabaný baðlantýsý appsettings.json içindeki DefaultConnection üzerinden kurulur.
builder.Services.AddDbContext<FoodWiseDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ASP.NET Identity kullanýcý ve rol sistemi eklenir.
// ApplicationUser özel kullanýcý sýnýfý, IdentityRole ise rol yönetimi için kullanýlýr.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<FoodWiseDbContext>()
    .AddDefaultTokenProviders();

// Auth iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IAuthService, AuthService>();

// Stok yönetimi iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IStockService, StockService>();

// Tarif listeleme, öneri ve etkileþim iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IRecipeService, RecipeService>();

// Paylaþým ilaný ve paylaþým talebi iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<ISharingService, SharingService>();

// QR destekli teslimat ve teslim kutusu iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IDeliveryService, DeliveryService>();

// Kullanýcý bildirim iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<INotificationService, NotificationService>();

// Karbon raporu oluþturma ve rapor görüntüleme iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<ICarbonReportService, CarbonReportService>();

// Kullanýcý profil iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IProfileService, ProfileService>();

// Tarif önerilerinde kiþiselleþtirilmiþ skor hesaplama iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IRecipeAiScoringService, RecipeAiScoringService>();

// Eco puan geçmiþi ve toplam eco puan hesaplama iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IEcoPointService, EcoPointService>();

// Paylaþým talebi oluþturulurken kullanýcý-ilan eþleþme skoru hesaplamak için servis kaydý yapýlýr.
builder.Services.AddScoped<IShareRequestMatchingService, ShareRequestMatchingService>();

// JSON tarif veri setini veritabanýna aktarmak için servis kaydý yapýlýr.
builder.Services.AddScoped<IRecipeDatasetImportService, RecipeDatasetImportService>();

// Admin panelindeki kategori, ürün, kullanýcý, teslim noktasý ve raporlama iþlemleri için servis kaydý yapýlýr.
builder.Services.AddScoped<IAdminService, AdminService>();

// Python FastAPI risk tahmin servisiyle haberleþmek için HttpClient kaydý yapýlýr.
builder.Services.AddHttpClient<IMlRiskPredictionService, MlRiskPredictionService>();

// Python FastAPI tarif öneri modeliyle haberleþmek için HttpClient kaydý yapýlýr.
builder.Services.AddHttpClient<IMlRecipeRecommendationService, MlRecipeRecommendationService>();

// Python FastAPI paylaþým eþleþtirme modeliyle haberleþmek için HttpClient kaydý yapýlýr.
builder.Services.AddHttpClient<IMlShareMatchingService, MlShareMatchingService>();

// JWT Authentication ayarlarý yapýlýr.
// [Authorize] kullanýlan endpointlerin token ile korunmasýný saðlar.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Token içerisindeki issuer, audience, süre ve imza bilgileri doðrulanýr.
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        // JWT imzasýný doðrulamak için appsettings.json içindeki gizli anahtar kullanýlýr.
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
        ),

        // JWT içindeki rol bilgisinin [Authorize(Roles = "Admin")] ile okunmasýný saðlar.
        RoleClaimType = ClaimTypes.Role
    };
});

// Rol bazlý yetkilendirme sistemi eklenir.
builder.Services.AddAuthorization();

var app = builder.Build();

// Uygulama ilk açýldýðýnda baþlangýç verileri oluþturulur.
// Admin/User rolleri ve varsayýlan admin kullanýcýsý bu aþamada eklenir.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FoodWiseDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await FoodWiseDbSeeder.SeedAsync(context, userManager, roleManager);
}

// Geliþtirme ortamýnda Swagger arayüzü aktif edilir.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTP istekleri HTTPS'e yönlendirilir.
app.UseHttpsRedirection();

// Önce authentication çalýþýr, kullanýcýnýn kimliði doðrulanýr.
app.UseAuthentication();

// Sonra authorization çalýþýr, kullanýcýnýn yetkisi kontrol edilir.
app.UseAuthorization();

// Controller endpointleri uygulamaya baðlanýr.
app.MapControllers();

app.Run();

