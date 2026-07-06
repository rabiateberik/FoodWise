
// FoodWise.Web MVC uygulamasýnýn baþlangýç ayarlarý bu dosyada yapýlýr.
// MVC yapýsý, Session yönetimi, API baðlantýsý ve Web servis kayýtlarý burada tanýmlanýr.

using FoodWise.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC controller ve view desteði eklenir.
builder.Services.AddControllersWithViews();

// Session kullanýmý için gerekli ayarlar yapýlýr.
// JWT token ve bazý kullanýcý bilgileri web tarafýnda Session içinde saklanýr.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// HttpContext bilgisine servisler içinden eriþebilmek için eklenir.
// Session, token veya kullanýcý bilgisi okuma iþlemlerinde kullanýlabilir.
builder.Services.AddHttpContextAccessor();

// Backend API ile haberleþmek için ortak HttpClient tanýmlanýr.
// API adresi appsettings.json içindeki ApiSettings:BaseUrl deðerinden okunur.
builder.Services.AddHttpClient("FoodWiseApi", client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

    if (string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanýmlanmalýdýr.");
    }

    client.BaseAddress = new Uri(apiBaseUrl);
});

// Web tarafýndaki login/register iþlemlerini API'ye gönderen servis kaydý yapýlýr.
builder.Services.AddScoped<IAuthWebService, AuthWebService>();

// Web servisleri HttpClient ile birlikte Dependency Injection container'a eklenir.
// Bu servisler MVC Controller ile Backend API arasýnda iletiþim kurar.
builder.Services.AddHttpClient<IStockWebService, StockWebService>();
builder.Services.AddHttpClient<IRecipeWebService, RecipeWebService>();
builder.Services.AddHttpClient<ISharingWebService, SharingWebService>();
builder.Services.AddHttpClient<INotificationWebService, NotificationWebService>();
builder.Services.AddHttpClient<ICarbonReportWebService, CarbonReportWebService>();
builder.Services.AddHttpClient<IDeliveryWebService, DeliveryWebService>();
builder.Services.AddHttpClient<IProfileWebService, ProfileWebService>();
builder.Services.AddHttpClient<IEcoPointWebService, EcoPointWebService>();
builder.Services.AddHttpClient<IAdminWebService, AdminWebService>();

var app = builder.Build();

// Production ortamýnda genel hata sayfasý ve HSTS güvenlik ayarý kullanýlýr.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// HTTP istekleri HTTPS'e yönlendirilir.
app.UseHttpsRedirection();

// wwwroot klasöründeki css, js ve görsel dosyalarýnýn kullanýlmasýný saðlar.
app.UseStaticFiles();

app.UseRouting();

// Session middleware'i aktif edilir.
// Route iþleminden sonra, authorization iþleminden önce çalýþmasý gerekir.
app.UseSession();

app.UseAuthorization();

// Varsayýlan route tanýmlanýr.
// Uygulama açýldýðýnda kullanýcý Auth/Login sayfasýna yönlendirilir.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();

