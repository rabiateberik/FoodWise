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

                City = "Ankara",
                District = "Çankaya",
                Neighborhood = "Merkez",

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
        if (await context.Categories.AnyAsync())
            return;

        var categories = new List<Category>
        {
            new() { Name = "Süt Ürünleri", Description = "Süt, yoğurt, peynir gibi ürünler" },
            new() { Name = "Sebze", Description = "Taze sebze ürünleri" },
            new() { Name = "Meyve", Description = "Taze meyve ürünleri" },
            new() { Name = "Et ve Balık", Description = "Et, tavuk ve balık ürünleri" },
            new() { Name = "Bakliyat", Description = "Kuru gıda ve bakliyat ürünleri" },
            new() { Name = "Unlu Mamuller", Description = "Ekmek, simit, hamur işi ürünleri" },
            new() { Name = "Diğer", Description = "Kullanıcı tarafından eklenen ve belirli kategoriye atanamayan ürünler" }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUnitsAsync(FoodWiseDbContext context)
    {
        if (await context.Units.AnyAsync())
            return;

        var units = new List<Unit>
        {
            new() { Name = "Kilogram", ShortName = "kg" },
            new() { Name = "Gram", ShortName = "gr" },
            new() { Name = "Litre", ShortName = "lt" },
            new() { Name = "Mililitre", ShortName = "ml" },
            new() { Name = "Adet", ShortName = "adet" },
            new() { Name = "Paket", ShortName = "paket" }
        };

        await context.Units.AddRangeAsync(units);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProductsAsync(FoodWiseDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var dairyCategory = await context.Categories.FirstAsync(x => x.Name == "Süt Ürünleri");
        var vegetableCategory = await context.Categories.FirstAsync(x => x.Name == "Sebze");
        var fruitCategory = await context.Categories.FirstAsync(x => x.Name == "Meyve");
        var bakeryCategory = await context.Categories.FirstAsync(x => x.Name == "Unlu Mamuller");
        var legumeCategory = await context.Categories.FirstAsync(x => x.Name == "Bakliyat");

        var products = new List<Product>
        {
            new()
            {
                Name = "Süt",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 7,
                OpenedShelfLifeDays = 3,
                CarbonFactor = 1.30m,
                IsSensitiveFood = true,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Yoğurt",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 14,
                OpenedShelfLifeDays = 5,
                CarbonFactor = 1.20m,
                IsSensitiveFood = true,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Peynir",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 30,
                OpenedShelfLifeDays = 7,
                CarbonFactor = 2.10m,
                IsSensitiveFood = true,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Yumurta",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 28,
                OpenedShelfLifeDays = null,
                CarbonFactor = 4.80m,
                IsSensitiveFood = true,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Domates",
                CategoryId = vegetableCategory.Id,
                DefaultShelfLifeDays = 7,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.40m,
                IsSensitiveFood = false,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Salatalık",
                CategoryId = vegetableCategory.Id,
                DefaultShelfLifeDays = 5,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.30m,
                IsSensitiveFood = false,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Muz",
                CategoryId = fruitCategory.Id,
                DefaultShelfLifeDays = 5,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.70m,
                IsSensitiveFood = false,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Ekmek",
                CategoryId = bakeryCategory.Id,
                DefaultShelfLifeDays = 3,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.60m,
                IsSensitiveFood = false,
                IsSystemDefined = true,
                IsApproved = true
            },
            new()
            {
                Name = "Pirinç",
                CategoryId = legumeCategory.Id,
                DefaultShelfLifeDays = 365,
                OpenedShelfLifeDays = null,
                CarbonFactor = 2.70m,
                IsSensitiveFood = false,
                IsSystemDefined = true,
                IsApproved = true
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDeliveryPointsAsync(FoodWiseDbContext context)
    {
        if (await context.DeliveryPoints.AnyAsync())
            return;

        var deliveryPoints = new List<DeliveryPoint>
{
    new()
    {
        Name = "Kampüs Kütüphane Girişi",
        Description = "Kampüs içinde güvenli teslim noktası",
        City = "Kayseri",
        District = "Talas",
        Neighborhood = "Kampüs",
        WorkingHours = "09:00 - 18:00",
        StorageType = "Oda sıcaklığı"
    },
    new()
    {
        Name = "Yurt Danışma Noktası",
        Description = "Öğrenci yurdu danışma alanı",
        City = "Kayseri",
        District = "Talas",
        Neighborhood = "Yurt Bölgesi",
        WorkingHours = "08:00 - 22:00",
        StorageType = "Oda sıcaklığı"
    },
    new()
    {
        Name = "Kafeterya Önü",
        Description = "Kampüs kafeteryası önündeki teslim noktası",
        City = "Kayseri",
        District = "Talas",
        Neighborhood = "Kampüs",
        WorkingHours = "10:00 - 17:00",
        StorageType = "Oda sıcaklığı"
    }
};

        await context.DeliveryPoints.AddRangeAsync(deliveryPoints);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDeliveryBoxesAsync(FoodWiseDbContext context)
    {
        if (await context.DeliveryBoxes.AnyAsync())
            return;

        var libraryPoint = await context.DeliveryPoints
            .FirstAsync(x => x.Name == "Kampüs Kütüphane Girişi");

        var dormPoint = await context.DeliveryPoints
            .FirstAsync(x => x.Name == "Yurt Danışma Noktası");

        var cafeteriaPoint = await context.DeliveryPoints
            .FirstAsync(x => x.Name == "Kafeterya Önü");

        var boxes = new List<DeliveryBox>
        {
            new()
            {
                DeliveryPointId = libraryPoint.Id,
                BoxCode = "B-01",
                QrCodeValue = "FW-DP-LIB-B01",
                Description = "Kampüs kütüphane girişindeki birinci teslim kutusu",
                IsOccupied = false
            },
            new()
            {
                DeliveryPointId = libraryPoint.Id,
                BoxCode = "B-02",
                QrCodeValue = "FW-DP-LIB-B02",
                Description = "Kampüs kütüphane girişindeki ikinci teslim kutusu",
                IsOccupied = false
            },
            new()
            {
                DeliveryPointId = dormPoint.Id,
                BoxCode = "Y-01",
                QrCodeValue = "FW-DP-DORM-Y01",
                Description = "Yurt danışma noktasındaki birinci teslim kutusu",
                IsOccupied = false
            },
            new()
            {
                DeliveryPointId = cafeteriaPoint.Id,
                BoxCode = "K-01",
                QrCodeValue = "FW-DP-CAF-K01",
                Description = "Kafeterya önündeki birinci teslim kutusu",
                IsOccupied = false
            }
        };

        await context.DeliveryBoxes.AddRangeAsync(boxes);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRecipesAsync(FoodWiseDbContext context)
    {
        if (await context.Recipes.AnyAsync())
            return;

        var adetUnit = await context.Units.FirstAsync(x => x.ShortName == "adet");
        var mlUnit = await context.Units.FirstAsync(x => x.ShortName == "ml");
        var grUnit = await context.Units.FirstAsync(x => x.ShortName == "gr");

        var yumurta = await context.Products.FirstAsync(x => x.Name == "Yumurta");
        var domates = await context.Products.FirstAsync(x => x.Name == "Domates");
        var sut = await context.Products.FirstAsync(x => x.Name == "Süt");
        var yogurt = await context.Products.FirstAsync(x => x.Name == "Yoğurt");
        var salatalik = await context.Products.FirstAsync(x => x.Name == "Salatalık");
        var ekmek = await context.Products.FirstAsync(x => x.Name == "Ekmek");

        var menemen = new Recipe
        {
            Name = "Menemen",
            Description = "Domates ve yumurta ile hazırlanan pratik yemek",
            Instructions = "Domatesleri doğrayın. Tavada pişirin. Yumurtaları ekleyip karıştırarak pişirin.",
            PreparationTimeMinutes = 15,
            SourceType = RecipeSourceType.Local
        };

        var omlet = new Recipe
        {
            Name = "Omlet",
            Description = "Yumurta ile hazırlanan hızlı kahvaltılık",
            Instructions = "Yumurtaları çırpın. Tavaya ekleyip pişirin. İsteğe göre peynir veya sebze ekleyin.",
            PreparationTimeMinutes = 10,
            SourceType = RecipeSourceType.Local
        };

        var krep = new Recipe
        {
            Name = "Krep",
            Description = "Süt ve yumurta ile hazırlanan pratik tarif",
            Instructions = "Süt, yumurta ve unu karıştırın. Tavada ince şekilde pişirin.",
            PreparationTimeMinutes = 20,
            SourceType = RecipeSourceType.Local
        };

        var cacik = new Recipe
        {
            Name = "Cacık",
            Description = "Yoğurt ve salatalık ile hazırlanan ferahlatıcı tarif",
            Instructions = "Yoğurdu çırpın. Salatalığı doğrayın veya rendeleyin. Karıştırıp servis edin.",
            PreparationTimeMinutes = 10,
            SourceType = RecipeSourceType.Local
        };

        var yumurtaliEkmek = new Recipe
        {
            Name = "Yumurtalı Ekmek",
            Description = "Bayat ekmekleri değerlendirmek için pratik tarif",
            Instructions = "Yumurtaları çırpın. Ekmekleri yumurtaya bulayıp tavada kızartın.",
            PreparationTimeMinutes = 15,
            SourceType = RecipeSourceType.Local
        };

        await context.Recipes.AddRangeAsync(menemen, omlet, krep, cacik, yumurtaliEkmek);
        await context.SaveChangesAsync();

        var recipeIngredients = new List<RecipeIngredient>
        {
            new() { RecipeId = menemen.Id, ProductId = yumurta.Id, UnitId = adetUnit.Id, Quantity = 2, IsRequired = true },
            new() { RecipeId = menemen.Id, ProductId = domates.Id, UnitId = adetUnit.Id, Quantity = 2, IsRequired = true },

            new() { RecipeId = omlet.Id, ProductId = yumurta.Id, UnitId = adetUnit.Id, Quantity = 2, IsRequired = true },

            new() { RecipeId = krep.Id, ProductId = sut.Id, UnitId = mlUnit.Id, Quantity = 250, IsRequired = true },
            new() { RecipeId = krep.Id, ProductId = yumurta.Id, UnitId = adetUnit.Id, Quantity = 1, IsRequired = true },

            new() { RecipeId = cacik.Id, ProductId = yogurt.Id, UnitId = grUnit.Id, Quantity = 250, IsRequired = true },
            new() { RecipeId = cacik.Id, ProductId = salatalik.Id, UnitId = adetUnit.Id, Quantity = 1, IsRequired = true },

            new() { RecipeId = yumurtaliEkmek.Id, ProductId = yumurta.Id, UnitId = adetUnit.Id, Quantity = 2, IsRequired = true },
            new() { RecipeId = yumurtaliEkmek.Id, ProductId = ekmek.Id, UnitId = adetUnit.Id, Quantity = 4, IsRequired = true }
        };

        await context.RecipeIngredients.AddRangeAsync(recipeIngredients);
        await context.SaveChangesAsync();
    }
}