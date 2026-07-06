# FoodWise

FoodWise, gıda israfını azaltmaya yardımcı olmak amacıyla geliştirilmiş web ve mobil tabanlı bir stok takip ve paylaşım uygulamasıdır.

Kullanıcılar stoklarındaki ürünleri takip edebilir, son kullanma tarihi yaklaşan ürünleri görebilir, mevcut ürünlerine göre tarif önerileri alabilir ve ihtiyaç fazlası ürünleri diğer kullanıcılarla paylaşabilir.

## Projenin Amacı

Evlerde ve öğrenci yaşamında gıda ürünleri çoğu zaman unutulabiliyor, son kullanma tarihleri takip edilemiyor ve kullanılmadan çöpe gidebiliyor.

FoodWise bu problemi azaltmak için stok takibi, risk tahmini, tarif önerisi ve güvenli paylaşım süreçlerini tek sistem altında birleştirir.

## Temel Özellikler

* Kullanıcı kayıt ve giriş işlemleri
* Stok ekleme, güncelleme ve silme
* Son kullanma tarihi takibi
* Riskli ürünlerin listelenmesi
* Yapay zekâ destekli risk tahmini
* Stoktaki ürünlere göre tarif önerileri
* Paylaşım ilanı oluşturma
* Paylaşım ilanlarına talep gönderme
* Kullanıcı-ilan eşleşme skoru
* Talep onaylama ve reddetme
* Teslimat ve QR kod süreci
* Bildirim sistemi
* Eco puan takibi
* Karbon tasarrufu raporları
* Web ve mobil uygulama desteği

## Kullanılan Teknolojiler

### Backend

* ASP.NET Core Web API
* C#
* Entity Framework Core
* ASP.NET Identity
* JWT Authentication

### Web

* ASP.NET MVC
* Razor View
* HTML
* CSS
* JavaScript

### Mobil

* Flutter
* Dart

### Veritabanı

* Microsoft SQL Server
* Entity Framework Core Code First

### Yapay Zekâ / Makine Öğrenmesi

* Python
* scikit-learn
* pandas
* NumPy
* FastAPI
* joblib / pickle

## Yapay Zekâ Modülleri

Projede üç farklı makine öğrenmesi modeli kullanılmaktadır.

### 1. Risk Tahmini

Stoktaki ürünün israf riskini tahmin eder.

**Algoritma:** `RandomForestClassifier`

Tahmin sınıfları:

* Low
* Medium
* High
* Critical

Model; son kullanma tarihi, ürünün açılıp açılmadığı, açılma süresi, saklama koşulu ve ürün özellikleri gibi verileri dikkate alır.

### 2. Tarif Önerisi

Kullanıcının stokundaki ürünlerle tarif malzemelerini karşılaştırarak tariflere uygunluk skoru verir.

**Algoritma:** `GradientBoostingRegressor`

Model, tarifleri 0-100 arasında puanlar. Malzeme eşleşme oranı, eksik malzeme sayısı ve kullanıcı etkileşimleri değerlendirmeye katılır.

### 3. Paylaşım Eşleşmesi

Paylaşım ilanı ile talep gönderen kullanıcı arasındaki uygunluk skorunu hesaplar.

**Algoritma:** `RandomForestRegressor`

Konum yakınlığı, ihtiyaç puanı, güvenilirlik ve geçmiş teslimat davranışları gibi bilgiler kullanılır.

## Sistem Mimarisi

Uygulama genel olarak aşağıdaki yapı üzerinden çalışır:

```text
Web Uygulaması
       |
Mobil Uygulama
       |
       v
ASP.NET Core API
       |
       +----> SQL Server
       |
       +----> FastAPI ML Servisi
```

Web ve mobil uygulama veritabanına doğrudan erişmez. Tüm işlemler ASP.NET Core API üzerinden yürütülür.

Backend tarafında controller ve service sorumlulukları ayrılmıştır. İş kuralları service katmanında, veritabanı işlemleri Entity Framework Core üzerinden yönetilir.

## Örnek Kullanım Akışı

Bir kullanıcı stokuna ürün eklediğinde:

1. Ürün bilgileri API'ye gönderilir.
2. Stok kaydı oluşturulur.
3. Risk tahmini yapılır.
4. Riskli ürünler ilgili ekranda gösterilir.
5. Kullanıcı isterse tarif önerisi alır.
6. İhtiyaç fazlası ürünü paylaşıma açabilir.
7. Başka bir kullanıcı ilana talep gönderebilir.
8. Talep için eşleşme skoru hesaplanır.
9. Talep onaylanırsa teslimat süreci başlatılır.

## Proje Yapısı

Proje temel olarak şu bölümlerden oluşur:

```text
backend/        ASP.NET Core API ve iş katmanı
web/            ASP.NET MVC / Razor web uygulaması
mobile/         Flutter mobil uygulaması
ml/             Python modelleri ve FastAPI servisi
```

Klasör isimleri repodaki mevcut yapıya göre değişebilir.

## Kurulum

### Backend

```bash
dotnet restore
dotnet ef database update
dotnet run
```

Veritabanı bağlantısı için ilgili `appsettings` dosyasındaki connection string düzenlenmelidir.

### Web

```bash
dotnet restore
dotnet run
```

Web uygulamasının kullandığı API adresi proje ayarlarından kontrol edilmelidir.

### Mobil

```bash
flutter pub get
flutter run
```

API adresi mobil uygulamadaki ilgili sabit veya configuration dosyasından ayarlanmalıdır.

### AI Servisi

Python bağımlılıkları kurulduktan sonra FastAPI servisi başlatılabilir.

```bash
pip install -r requirements.txt
uvicorn main:app --reload
```

Eğitilmiş model dosyalarının servis tarafından erişilebilir konumda bulunması gerekir.

## Veritabanı

Projede SQL Server kullanılmıştır. Temel tablolar arasında:

* Users
* Products
* Categories
* StockItems
* WasteRiskPredictions
* Recipes
* RecipeIngredients
* RecipeRecommendations
* ShareListings
* ShareRequests
* Deliveries
* Notifications
* EcoPointHistories
* CarbonReports

yer almaktadır.

## Not

Bu proje bitirme projesi kapsamında geliştirilmiştir. Amaç yalnızca stok takibi yapmak değil; stok yönetimi, yapay zekâ destekli öneriler ve güvenli paylaşım süreçlerini tek bir sistem altında birleştirmektir.
