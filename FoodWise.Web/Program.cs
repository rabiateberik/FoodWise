// Bu dosya, FoodWise.Web MVC uygulamasýnýn baþlangýç ayarlarýný yapar.
// MVC servisleri, Session yönetimi ve API ile haberleþmek için HttpClient burada yapýlandýrýlýr.

using FoodWise.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC controller ve view desteði eklenir.
builder.Services.AddControllersWithViews();

// Session kullanabilmek için gerekli servisler eklenir.
// JWT token ve kullanýcý bilgileri web tarafýnda Session içinde saklanacaktýr.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60); // Kullanýcý oturumu 60 dakika aktif kalýr.
    options.Cookie.HttpOnly = true;                 // Cookie'ye client-side script eriþimini engeller.
    options.Cookie.IsEssential = true;              // Session cookie'sinin zorunlu olduðunu belirtir.
});

// HttpContext'e servis katmanlarýndan eriþebilmek için eklenir.
// Ýleride token okuma, kullanýcý bilgisi alma gibi iþlemlerde kullanýlabilir.
builder.Services.AddHttpContextAccessor();

// API ile haberleþmek için HttpClient tanýmlanýr.
// BaseAddress, appsettings.json içindeki ApiSettings:BaseUrl deðerinden okunur.
builder.Services.AddHttpClient("FoodWiseApi", client =>
{
    var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"];

    if (string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        throw new InvalidOperationException("ApiSettings:BaseUrl appsettings.json içinde tanýmlanmalýdýr.");
    }

    client.BaseAddress = new Uri(apiBaseUrl);
});
// AuthWebService, Web tarafýndaki login/register iþlemlerinin API'ye gönderilmesini saðlar.
builder.Services.AddScoped<IAuthWebService, AuthWebService>();
// Stock API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<IStockWebService, StockWebService>();
// Recipe API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<IRecipeWebService, RecipeWebService>();
// Sharing API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<ISharingWebService, SharingWebService>();
// Notification API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<INotificationWebService, NotificationWebService>();
// CarbonReport API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<ICarbonReportWebService, CarbonReportWebService>();
// Delivery API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<IDeliveryWebService, DeliveryWebService>();
// Profile API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr.
builder.Services.AddHttpClient<IProfileWebService, ProfileWebService>();
// Eco puan özetini ve puan geçmiþini API'den çekmek için Web servis kaydý.
builder.Services.AddHttpClient<IEcoPointWebService, EcoPointWebService>();
//Admin API ile haberleþen Web servisinin HttpClient baðýmlýlýðý burada tanýmlanýr. Admin paneli iþlemleri için kullanýlýr.
builder.Services.AddHttpClient<IAdminWebService, AdminWebService>();
var app = builder.Build();

// Production ortamýnda hata sayfasý yönetimi yapýlýr.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// wwwroot içindeki css, js, image gibi statik dosyalarýn kullanýlmasýný saðlar.
app.UseStaticFiles();

app.UseRouting();

// Session middleware'i route iþleminden sonra, authorization iþleminden önce kullanýlmalýdýr.
app.UseSession();

app.UseAuthorization();

// Varsayýlan route ayarý.
// Uygulama açýldýðýnda kullanýcý Auth/Login sayfasýna yönlendirilecektir.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();