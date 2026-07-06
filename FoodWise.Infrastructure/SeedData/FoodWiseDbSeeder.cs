// FoodWiseDbSeeder, uygulama ilk çalıştığında temel verileri veritabanına ekler.
// Kategori, birim, ürün, teslim noktası, teslim kutusu, örnek tarif ve admin kullanıcı verileri burada oluşturulur.

using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using FoodWise.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.SeedData;

public static class FoodWiseDbSeeder
{
    private const string AdminRoleName = "Admin";
    private const string UserRoleName = "User";

    private const string AdminEmail = "admin@foodwise.com";
    private const string AdminPassword = "Admin123*";

    public static async Task SeedAsync(
        FoodWiseDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        await context.Database.MigrateAsync();

        await SeedRolesAsync(roleManager);
        await SeedAdminUserAsync(userManager);

        await SeedCategoriesAsync(context);
        await SeedUnitsAsync(context);
        await SeedProductsAsync(context);
        await SeedDeliveryPointsAsync(context);
        await SeedDeliveryBoxesAsync(context);
        await SeedRecipesAsync(context);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new List<string>
        {
            AdminRoleName,
            UserRoleName
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager)
    {
        var adminUser = await userManager.FindByEmailAsync(AdminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                FullName = "FoodWise Admin",
                Email = AdminEmail,
                UserName = AdminEmail,
                EmailConfirmed = true,

                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Mevlana",

                NeedScore = 0,
                ReliabilityScore = 100,

                IsActive = true,
                CreatedAt = DateTime.Now
            };

            var createResult = await userManager.CreateAsync(adminUser, AdminPassword);

            if (!createResult.Succeeded)
            {
                var errors = string.Join(" | ", createResult.Errors.Select(x => x.Description));
                throw new InvalidOperationException($"Admin kullanıcı oluşturulamadı: {errors}");
            }
        }
        else
        {
            adminUser.IsActive = true;
            adminUser.DeletedAt = null;
            adminUser.EmailConfirmed = true;
            adminUser.ReliabilityScore = adminUser.ReliabilityScore <= 0 ? 100 : adminUser.ReliabilityScore;
            adminUser.UpdatedAt = DateTime.Now;

            await userManager.UpdateAsync(adminUser);
        }

        if (!await userManager.IsInRoleAsync(adminUser, AdminRoleName))
        {
            await userManager.AddToRoleAsync(adminUser, AdminRoleName);
        }

        if (!await userManager.IsInRoleAsync(adminUser, UserRoleName))
        {
            await userManager.AddToRoleAsync(adminUser, UserRoleName);
        }
    }

    private static async Task SeedCategoriesAsync(FoodWiseDbContext context)
    {
        var categories = new List<Category>
        {
            new() { Name = "Süt Ürünleri", Description = "Süt, yoğurt, peynir gibi ürünler" },
            new() { Name = "Sebze", Description = "Taze sebze ürünleri" },
            new() { Name = "Meyve", Description = "Taze meyve ürünleri" },
            new() { Name = "Et ve Balık", Description = "Et, tavuk ve balık ürünleri" },
            new() { Name = "Bakliyat", Description = "Kuru gıda ve bakliyat ürünleri" },
            new() { Name = "Temel Gıda", Description = "Pirinç, bulgur, makarna, un gibi temel mutfak ürünleri" },
            new() { Name = "Unlu Mamuller", Description = "Ekmek, lavaş, tost ekmeği ve benzeri ürünler" },
            new() { Name = "İçecek", Description = "Ayran, meyve suyu ve benzeri içecekler" },
            new() { Name = "Diğer", Description = "Kullanıcı tarafından eklenen ve belirli kategoriye atanamayan ürünler" }
        };

        foreach (var category in categories)
        {
            var exists = await context.Categories.AnyAsync(x => x.Name == category.Name);

            if (!exists)
            {
                await context.Categories.AddAsync(category);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUnitsAsync(FoodWiseDbContext context)
    {
        var units = new List<Unit>
        {
            new() { Name = "Kilogram", ShortName = "kg" },
            new() { Name = "Gram", ShortName = "gr" },
            new() { Name = "Litre", ShortName = "lt" },
            new() { Name = "Mililitre", ShortName = "ml" },
            new() { Name = "Adet", ShortName = "adet" },
            new() { Name = "Paket", ShortName = "paket" }
        };

        foreach (var unit in units)
        {
            var exists = await context.Units.AnyAsync(x => x.ShortName == unit.ShortName);

            if (!exists)
            {
                await context.Units.AddAsync(unit);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(FoodWiseDbContext context)
    {
        var dairyCategory = await context.Categories.FirstAsync(x => x.Name == "Süt Ürünleri");
        var vegetableCategory = await context.Categories.FirstAsync(x => x.Name == "Sebze");
        var fruitCategory = await context.Categories.FirstAsync(x => x.Name == "Meyve");
        var meatCategory = await context.Categories.FirstAsync(x => x.Name == "Et ve Balık");
        var bakeryCategory = await context.Categories.FirstAsync(x => x.Name == "Unlu Mamuller");
        var legumeCategory = await context.Categories.FirstAsync(x => x.Name == "Bakliyat");
        var basicFoodCategory = await context.Categories.FirstAsync(x => x.Name == "Temel Gıda");
        var drinkCategory = await context.Categories.FirstAsync(x => x.Name == "İçecek");

        var products = new List<Product>
        {
            // Süt ürünleri
            new() { Name = "Süt", CategoryId = dairyCategory.Id, DefaultShelfLifeDays = 7, OpenedShelfLifeDays = 3, CarbonFactor = 1.30m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Yoğurt", CategoryId = dairyCategory.Id, DefaultShelfLifeDays = 14, OpenedShelfLifeDays = 5, CarbonFactor = 1.20m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Peynir", CategoryId = dairyCategory.Id, DefaultShelfLifeDays = 30, OpenedShelfLifeDays = 7, CarbonFactor = 2.10m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Yumurta", CategoryId = dairyCategory.Id, DefaultShelfLifeDays = 28, OpenedShelfLifeDays = null, CarbonFactor = 4.80m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Tereyağı", CategoryId = dairyCategory.Id, DefaultShelfLifeDays = 45, OpenedShelfLifeDays = 15, CarbonFactor = 3.40m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Kaşar Peyniri", CategoryId = dairyCategory.Id, DefaultShelfLifeDays = 45, OpenedShelfLifeDays = 10, CarbonFactor = 2.80m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },

            // Sebze
            new() { Name = "Domates", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 7, OpenedShelfLifeDays = null, CarbonFactor = 0.40m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Salatalık", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 5, OpenedShelfLifeDays = null, CarbonFactor = 0.30m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Biber", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 7, OpenedShelfLifeDays = null, CarbonFactor = 0.35m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Patates", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 30, OpenedShelfLifeDays = null, CarbonFactor = 0.25m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Soğan", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 45, OpenedShelfLifeDays = null, CarbonFactor = 0.20m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Havuç", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 20, OpenedShelfLifeDays = null, CarbonFactor = 0.30m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Kabak", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 10, OpenedShelfLifeDays = null, CarbonFactor = 0.28m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Marul", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 5, OpenedShelfLifeDays = null, CarbonFactor = 0.25m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Ispanak", CategoryId = vegetableCategory.Id, DefaultShelfLifeDays = 4, OpenedShelfLifeDays = null, CarbonFactor = 0.35m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },

            // Meyve
            new() { Name = "Elma", CategoryId = fruitCategory.Id, DefaultShelfLifeDays = 20, OpenedShelfLifeDays = null, CarbonFactor = 0.35m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Muz", CategoryId = fruitCategory.Id, DefaultShelfLifeDays = 5, OpenedShelfLifeDays = null, CarbonFactor = 0.70m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Portakal", CategoryId = fruitCategory.Id, DefaultShelfLifeDays = 14, OpenedShelfLifeDays = null, CarbonFactor = 0.45m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Çilek", CategoryId = fruitCategory.Id, DefaultShelfLifeDays = 4, OpenedShelfLifeDays = null, CarbonFactor = 0.55m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Üzüm", CategoryId = fruitCategory.Id, DefaultShelfLifeDays = 7, OpenedShelfLifeDays = null, CarbonFactor = 0.50m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },

            // Et ve balık
            new() { Name = "Tavuk Göğsü", CategoryId = meatCategory.Id, DefaultShelfLifeDays = 3, OpenedShelfLifeDays = 1, CarbonFactor = 6.90m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Kıyma", CategoryId = meatCategory.Id, DefaultShelfLifeDays = 2, OpenedShelfLifeDays = 1, CarbonFactor = 12.00m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Balık", CategoryId = meatCategory.Id, DefaultShelfLifeDays = 2, OpenedShelfLifeDays = 1, CarbonFactor = 5.40m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },

            // Bakliyat
            new() { Name = "Mercimek", CategoryId = legumeCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 0.90m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Nohut", CategoryId = legumeCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 1.00m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Kuru Fasulye", CategoryId = legumeCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 1.10m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },

            // Temel gıda
            new() { Name = "Pirinç", CategoryId = basicFoodCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 2.70m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Bulgur", CategoryId = basicFoodCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 1.20m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Makarna", CategoryId = basicFoodCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 1.10m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Un", CategoryId = basicFoodCategory.Id, DefaultShelfLifeDays = 365, OpenedShelfLifeDays = null, CarbonFactor = 0.80m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },

            // Unlu mamuller
            new() { Name = "Ekmek", CategoryId = bakeryCategory.Id, DefaultShelfLifeDays = 3, OpenedShelfLifeDays = null, CarbonFactor = 0.60m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Lavaş", CategoryId = bakeryCategory.Id, DefaultShelfLifeDays = 7, OpenedShelfLifeDays = 3, CarbonFactor = 0.70m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Tost Ekmeği", CategoryId = bakeryCategory.Id, DefaultShelfLifeDays = 10, OpenedShelfLifeDays = 4, CarbonFactor = 0.75m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true },

            // İçecek
            new() { Name = "Ayran", CategoryId = drinkCategory.Id, DefaultShelfLifeDays = 10, OpenedShelfLifeDays = 2, CarbonFactor = 0.90m, IsSensitiveFood = true, IsSystemDefined = true, IsApproved = true },
            new() { Name = "Meyve Suyu", CategoryId = drinkCategory.Id, DefaultShelfLifeDays = 180, OpenedShelfLifeDays = 5, CarbonFactor = 0.80m, IsSensitiveFood = false, IsSystemDefined = true, IsApproved = true }
        };

        foreach (var product in products)
        {
            var exists = await context.Products.AnyAsync(x => x.Name == product.Name);

            if (!exists)
            {
                await context.Products.AddAsync(product);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDeliveryPointsAsync(FoodWiseDbContext context)
    {
        var deliveryPoints = new List<DeliveryPoint>
        {
            // Talas
            new()
            {
                Name = "Mevlana Mahallesi Öğrenci Yaşam Merkezi A Blok",
                Description = "Mevlana Mahallesi içinde öğrenci kullanımına uygun demo teslim noktası",
                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Mevlana",
                WorkingHours = "09:00 - 22:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Bahçelievler Site Girişi B Blok",
                Description = "Bahçelievler Mahallesi site girişinde demo teslim noktası",
                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Bahçelievler",
                WorkingHours = "08:00 - 21:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Yenidoğan Yurt Danışma C Blok",
                Description = "Yenidoğan Mahallesi yurt danışma alanında demo teslim noktası",
                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Yenidoğan",
                WorkingHours = "08:00 - 23:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Kiçiköy Mahalle Evi A Blok",
                Description = "Kiçiköy Mahallesi mahalle evi girişinde demo teslim noktası",
                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Kiçiköy",
                WorkingHours = "10:00 - 18:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Harman Apartmanları B Blok",
                Description = "Harman Mahallesi apartmanlar bölgesinde demo teslim noktası",
                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Harman",
                WorkingHours = "09:00 - 20:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Tablakaya Sosyal Tesis C Blok",
                Description = "Tablakaya Mahallesi sosyal tesis çevresinde demo teslim noktası",
                City = "Kayseri",
                District = "Talas",
                Neighborhood = "Tablakaya",
                WorkingHours = "09:00 - 19:00",
                StorageType = "Oda sıcaklığı"
            },

            // Melikgazi
            new()
            {
                Name = "Köşk Mahallesi Site Girişi A Blok",
                Description = "Köşk Mahallesi site girişinde demo teslim noktası",
                City = "Kayseri",
                District = "Melikgazi",
                Neighborhood = "Köşk",
                WorkingHours = "09:00 - 21:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Selçuklu Yaşam Merkezi B Blok",
                Description = "Selçuklu Mahallesi yaşam merkezi girişinde demo teslim noktası",
                City = "Kayseri",
                District = "Melikgazi",
                Neighborhood = "Selçuklu",
                WorkingHours = "09:00 - 20:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "İldem Cumhuriyet Toplu Konut C Blok",
                Description = "İldem Cumhuriyet Mahallesi toplu konut alanında demo teslim noktası",
                City = "Kayseri",
                District = "Melikgazi",
                Neighborhood = "İldem Cumhuriyet",
                WorkingHours = "08:00 - 22:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Yıldırım Beyazıt Mahalle Evi A Blok",
                Description = "Yıldırım Beyazıt Mahallesi mahalle evi girişinde demo teslim noktası",
                City = "Kayseri",
                District = "Melikgazi",
                Neighborhood = "Yıldırım Beyazıt",
                WorkingHours = "10:00 - 18:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Esenyurt Apartmanları B Blok",
                Description = "Esenyurt Mahallesi apartmanlar bölgesinde demo teslim noktası",
                City = "Kayseri",
                District = "Melikgazi",
                Neighborhood = "Esenyurt",
                WorkingHours = "09:00 - 20:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Gesi Fatih Kültür Evi C Blok",
                Description = "Gesi Fatih Mahallesi kültür evi çevresinde demo teslim noktası",
                City = "Kayseri",
                District = "Melikgazi",
                Neighborhood = "Gesi Fatih",
                WorkingHours = "09:00 - 18:00",
                StorageType = "Oda sıcaklığı"
            }
        };

        foreach (var deliveryPoint in deliveryPoints)
        {
            var exists = await context.DeliveryPoints.AnyAsync(x => x.Name == deliveryPoint.Name);

            if (!exists)
            {
                await context.DeliveryPoints.AddAsync(deliveryPoint);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedDeliveryBoxesAsync(FoodWiseDbContext context)
    {
        async Task AddBoxIfNotExistsAsync(
            string pointName,
            string boxCode,
            string qrCodeValue,
            string description)
        {
            var deliveryPoint = await context.DeliveryPoints
                .FirstOrDefaultAsync(x => x.Name == pointName);

            if (deliveryPoint == null)
                return;

            var exists = await context.DeliveryBoxes
                .AnyAsync(x => x.QrCodeValue == qrCodeValue);

            if (exists)
                return;

            await context.DeliveryBoxes.AddAsync(new DeliveryBox
            {
                DeliveryPointId = deliveryPoint.Id,
                BoxCode = boxCode,
                QrCodeValue = qrCodeValue,
                Description = description,
                IsOccupied = false
            });
        }

        await AddBoxIfNotExistsAsync(
            "Mevlana Mahallesi Öğrenci Yaşam Merkezi A Blok",
            "M-A01",
            "FW-TAL-MEV-A01",
            "Mevlana Mahallesi Öğrenci Yaşam Merkezi A Blok birinci teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Mevlana Mahallesi Öğrenci Yaşam Merkezi A Blok",
            "M-A02",
            "FW-TAL-MEV-A02",
            "Mevlana Mahallesi Öğrenci Yaşam Merkezi A Blok ikinci teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Bahçelievler Site Girişi B Blok",
            "B-B01",
            "FW-TAL-BAH-B01",
            "Bahçelievler Site Girişi B Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Yenidoğan Yurt Danışma C Blok",
            "Y-C01",
            "FW-TAL-YEN-C01",
            "Yenidoğan Yurt Danışma C Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Kiçiköy Mahalle Evi A Blok",
            "K-A01",
            "FW-TAL-KIC-A01",
            "Kiçiköy Mahalle Evi A Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Harman Apartmanları B Blok",
            "H-B01",
            "FW-TAL-HAR-B01",
            "Harman Apartmanları B Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Tablakaya Sosyal Tesis C Blok",
            "T-C01",
            "FW-TAL-TAB-C01",
            "Tablakaya Sosyal Tesis C Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Köşk Mahallesi Site Girişi A Blok",
            "KOS-A01",
            "FW-MEL-KOS-A01",
            "Köşk Mahallesi Site Girişi A Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Selçuklu Yaşam Merkezi B Blok",
            "S-B01",
            "FW-MEL-SEL-B01",
            "Selçuklu Yaşam Merkezi B Blok birinci teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Selçuklu Yaşam Merkezi B Blok",
            "S-B02",
            "FW-MEL-SEL-B02",
            "Selçuklu Yaşam Merkezi B Blok ikinci teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "İldem Cumhuriyet Toplu Konut C Blok",
            "I-C01",
            "FW-MEL-ILD-C01",
            "İldem Cumhuriyet Toplu Konut C Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Yıldırım Beyazıt Mahalle Evi A Blok",
            "YB-A01",
            "FW-MEL-YIL-A01",
            "Yıldırım Beyazıt Mahalle Evi A Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Esenyurt Apartmanları B Blok",
            "E-B01",
            "FW-MEL-ESE-B01",
            "Esenyurt Apartmanları B Blok teslim kutusu");

        await AddBoxIfNotExistsAsync(
            "Gesi Fatih Kültür Evi C Blok",
            "GF-C01",
            "FW-MEL-GF-C01",
            "Gesi Fatih Kültür Evi C Blok teslim kutusu");

        await context.SaveChangesAsync();
    }

    private static async Task SeedRecipesAsync(FoodWiseDbContext context)
    {
        var adetUnit = await context.Units.FirstAsync(x => x.ShortName == "adet");
        var mlUnit = await context.Units.FirstAsync(x => x.ShortName == "ml");
        var grUnit = await context.Units.FirstAsync(x => x.ShortName == "gr");

        async Task<Product> ProductAsync(string name)
        {
            return await context.Products.FirstAsync(x => x.Name == name);
        }

        async Task AddRecipeIfNotExistsAsync(
            string name,
            string description,
            string instructions,
            int preparationTimeMinutes,
            List<(string ProductName, int UnitId, decimal Quantity, bool IsRequired)> ingredients)
        {
            var recipeExists = await context.Recipes.AnyAsync(x => x.Name == name);

            if (recipeExists)
                return;

            var recipe = new Recipe
            {
                Name = name,
                Description = description,
                Instructions = instructions,
                PreparationTimeMinutes = preparationTimeMinutes,
                SourceType = RecipeSourceType.Local
            };

            await context.Recipes.AddAsync(recipe);
            await context.SaveChangesAsync();

            var recipeIngredients = new List<RecipeIngredient>();

            foreach (var ingredient in ingredients)
            {
                var product = await ProductAsync(ingredient.ProductName);

                recipeIngredients.Add(new RecipeIngredient
                {
                    RecipeId = recipe.Id,
                    ProductId = product.Id,
                    UnitId = ingredient.UnitId,
                    Quantity = ingredient.Quantity,
                    IsRequired = ingredient.IsRequired
                });
            }

            await context.RecipeIngredients.AddRangeAsync(recipeIngredients);
            await context.SaveChangesAsync();
        }

        await AddRecipeIfNotExistsAsync(
            "Menemen",
            "Domates, biber ve yumurta ile hazırlanan pratik kahvaltılık",
            "Domates ve biberleri doğrayın. Tavada pişirin. Yumurtaları ekleyip karıştırarak pişirin.",
            15,
            new List<(string, int, decimal, bool)>
            {
                ("Yumurta", adetUnit.Id, 2, true),
                ("Domates", adetUnit.Id, 2, true),
                ("Biber", adetUnit.Id, 1, false)
            });

        await AddRecipeIfNotExistsAsync(
            "Omlet",
            "Yumurta ve peynir ile hazırlanan hızlı kahvaltılık",
            "Yumurtaları çırpın. Tavaya ekleyip pişirin. İsteğe göre peynir ekleyerek servis edin.",
            10,
            new List<(string, int, decimal, bool)>
            {
                ("Yumurta", adetUnit.Id, 2, true),
                ("Peynir", grUnit.Id, 50, false)
            });

        await AddRecipeIfNotExistsAsync(
            "Krep",
            "Süt, yumurta ve un ile hazırlanan pratik tarif",
            "Süt, yumurta ve unu karıştırın. Tavada ince şekilde pişirin.",
            20,
            new List<(string, int, decimal, bool)>
            {
                ("Süt", mlUnit.Id, 250, true),
                ("Yumurta", adetUnit.Id, 1, true),
                ("Un", grUnit.Id, 150, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Cacık",
            "Yoğurt ve salatalık ile hazırlanan ferahlatıcı tarif",
            "Yoğurdu çırpın. Salatalığı doğrayın veya rendeleyin. Karıştırıp servis edin.",
            10,
            new List<(string, int, decimal, bool)>
            {
                ("Yoğurt", grUnit.Id, 250, true),
                ("Salatalık", adetUnit.Id, 1, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Yumurtalı Ekmek",
            "Bayat ekmekleri değerlendirmek için pratik tarif",
            "Yumurtaları çırpın. Ekmekleri yumurtaya bulayıp tavada kızartın.",
            15,
            new List<(string, int, decimal, bool)>
            {
                ("Yumurta", adetUnit.Id, 2, true),
                ("Ekmek", adetUnit.Id, 4, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Mercimek Çorbası",
            "Mercimek, soğan ve havuç ile hazırlanan besleyici çorba",
            "Mercimekleri yıkayın. Soğan ve havuçla birlikte haşlayın. Blenderdan geçirip servis edin.",
            35,
            new List<(string, int, decimal, bool)>
            {
                ("Mercimek", grUnit.Id, 200, true),
                ("Soğan", adetUnit.Id, 1, true),
                ("Havuç", adetUnit.Id, 1, false)
            });

        await AddRecipeIfNotExistsAsync(
            "Sebzeli Omlet",
            "Yumurta, domates ve biber ile hazırlanan sebzeli omlet",
            "Sebzeleri doğrayıp tavada hafif pişirin. Yumurtayı ekleyip omlet kıvamında pişirin.",
            15,
            new List<(string, int, decimal, bool)>
            {
                ("Yumurta", adetUnit.Id, 2, true),
                ("Domates", adetUnit.Id, 1, false),
                ("Biber", adetUnit.Id, 1, false)
            });

        await AddRecipeIfNotExistsAsync(
            "Tavuklu Pilav",
            "Tavuk ve pirinç ile hazırlanan doyurucu ana yemek",
            "Tavuğu haşlayın. Pirinci kavurup su ekleyerek pişirin. Tavuk parçalarıyla servis edin.",
            45,
            new List<(string, int, decimal, bool)>
            {
                ("Tavuk Göğsü", grUnit.Id, 250, true),
                ("Pirinç", grUnit.Id, 200, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Sebzeli Makarna",
            "Makarna, domates ve biber ile hazırlanan pratik yemek",
            "Makarnayı haşlayın. Domates ve biberle sos hazırlayıp makarna ile karıştırın.",
            25,
            new List<(string, int, decimal, bool)>
            {
                ("Makarna", grUnit.Id, 200, true),
                ("Domates", adetUnit.Id, 2, false),
                ("Biber", adetUnit.Id, 1, false)
            });

        await AddRecipeIfNotExistsAsync(
            "Yoğurtlu Kabak",
            "Kabak ve yoğurt ile hazırlanan hafif yemek",
            "Kabakları doğrayıp pişirin. Soğuduktan sonra yoğurt ile karıştırıp servis edin.",
            25,
            new List<(string, int, decimal, bool)>
            {
                ("Kabak", adetUnit.Id, 2, true),
                ("Yoğurt", grUnit.Id, 200, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Patatesli Yumurta",
            "Patates ve yumurta ile hazırlanan ekonomik tarif",
            "Patatesleri doğrayıp tavada pişirin. Üzerine yumurta ekleyerek servis edin.",
            20,
            new List<(string, int, decimal, bool)>
            {
                ("Patates", adetUnit.Id, 2, true),
                ("Yumurta", adetUnit.Id, 2, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Ayranlı Pratik Öğün",
            "Ayran, ekmek ve peynir ile hazırlanabilen hızlı öğün önerisi",
            "Peynir ve ekmeği porsiyonlayın. Ayran ile birlikte servis edin.",
            5,
            new List<(string, int, decimal, bool)>
            {
                ("Ayran", mlUnit.Id, 250, true),
                ("Ekmek", adetUnit.Id, 2, false),
                ("Peynir", grUnit.Id, 50, false)
            });

        await AddRecipeIfNotExistsAsync(
            "Ispanaklı Yumurta",
            "Ispanak ve yumurta ile hazırlanan besleyici tarif",
            "Ispanağı yıkayıp doğrayın. Tavada hafif pişirdikten sonra yumurta ekleyin.",
            20,
            new List<(string, int, decimal, bool)>
            {
                ("Ispanak", grUnit.Id, 250, true),
                ("Yumurta", adetUnit.Id, 2, true)
            });

        await AddRecipeIfNotExistsAsync(
            "Tavuklu Lavaş",
            "Tavuk, lavaş ve sebzelerle hazırlanan pratik öğün",
            "Tavuğu pişirin. Lavaşın içine tavuk ve sebzeleri ekleyerek sarın.",
            30,
            new List<(string, int, decimal, bool)>
            {
                ("Tavuk Göğsü", grUnit.Id, 200, true),
                ("Lavaş", adetUnit.Id, 2, true),
                ("Marul", adetUnit.Id, 2, false),
                ("Domates", adetUnit.Id, 1, false)
            });
    }
}
