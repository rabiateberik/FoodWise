using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.SeedData;
// FoodWiseDbSeeder sınıfı, uygulama ilk çalıştığında veritabanına başlangıç verilerini eklemek için oluşturulmuştur.
// Bu yapı sayesinde kategori, birim, ürün, güvenli teslim noktası ve örnek tarif verileri manuel girilmeden otomatik olarak eklenir.
// Seed işlemi, veritabanının temel verilerle hazır gelmesini sağlar ve stok, tarif önerisi, paylaşım gibi modüllerin test edilmesini kolaylaştırır.
public static class FoodWiseDbSeeder
{
    public static async Task SeedAsync(FoodWiseDbContext context)
    {
        await context.Database.MigrateAsync();

        await SeedCategoriesAsync(context);
        await SeedUnitsAsync(context);
        await SeedProductsAsync(context);
        // Teslim noktaları eklendikten sonra bu noktalara ait kutular eklenir.
        // DeliveryBoxes, DeliveryPoints tablosuna bağlı olduğu için sıralama önemlidir.
        await SeedDeliveryPointsAsync(context);
        await SeedDeliveryBoxesAsync(context);

        await SeedRecipesAsync(context);

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
            new() { Name = "Unlu Mamuller", Description = "Ekmek, simit, hamur işi ürünleri" }
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
                IsSensitiveFood = true
            },
            new()
            {
                Name = "Yoğurt",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 14,
                OpenedShelfLifeDays = 5,
                CarbonFactor = 1.20m,
                IsSensitiveFood = true
            },
            new()
            {
                Name = "Peynir",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 30,
                OpenedShelfLifeDays = 7,
                CarbonFactor = 2.10m,
                IsSensitiveFood = true
            },
            new()
            {
                Name = "Yumurta",
                CategoryId = dairyCategory.Id,
                DefaultShelfLifeDays = 28,
                OpenedShelfLifeDays = null,
                CarbonFactor = 4.80m,
                IsSensitiveFood = true
            },
            new()
            {
                Name = "Domates",
                CategoryId = vegetableCategory.Id,
                DefaultShelfLifeDays = 7,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.40m,
                IsSensitiveFood = false
            },
            new()
            {
                Name = "Salatalık",
                CategoryId = vegetableCategory.Id,
                DefaultShelfLifeDays = 5,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.30m,
                IsSensitiveFood = false
            },
            new()
            {
                Name = "Muz",
                CategoryId = fruitCategory.Id,
                DefaultShelfLifeDays = 5,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.70m,
                IsSensitiveFood = false
            },
            new()
            {
                Name = "Ekmek",
                CategoryId = bakeryCategory.Id,
                DefaultShelfLifeDays = 3,
                OpenedShelfLifeDays = null,
                CarbonFactor = 0.60m,
                IsSensitiveFood = false
            },
            new()
            {
                Name = "Pirinç",
                CategoryId = legumeCategory.Id,
                DefaultShelfLifeDays = 365,
                OpenedShelfLifeDays = null,
                CarbonFactor = 2.70m,
                IsSensitiveFood = false
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
                Neighborhood = "Kampüs",
                WorkingHours = "09:00 - 18:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Yurt Danışma Noktası",
                Description = "Öğrenci yurdu danışma alanı",
                Neighborhood = "Yurt Bölgesi",
                WorkingHours = "08:00 - 22:00",
                StorageType = "Oda sıcaklığı"
            },
            new()
            {
                Name = "Kafeterya Önü",
                Description = "Kampüs kafeteryası önündeki teslim noktası",
                Neighborhood = "Kampüs",
                WorkingHours = "10:00 - 17:00",
                StorageType = "Oda sıcaklığı"
            }
        };

        await context.DeliveryPoints.AddRangeAsync(deliveryPoints);
        await context.SaveChangesAsync();
    }
    // Kontrollü teslim noktalarına ait örnek teslim kutularını veritabanına ekler.
    // Her kutunun sabit bir QR değeri vardır; alıcı bu QR ile teslimatı doğrulayacaktır.
    private static async Task SeedDeliveryBoxesAsync(FoodWiseDbContext context)
    {
        // Debug amaçlı: API çalışınca bu metodun tetiklenip tetiklenmediğini görmek için yazıldı.
        Console.WriteLine(">>> SeedDeliveryBoxesAsync çalıştı.");
        // Eğer kutular daha önce eklenmişse tekrar ekleme yapılmaz.
        if (await context.DeliveryBoxes.AnyAsync())
            return;

        // Kutular, daha önce seed edilen teslim noktalarına bağlanır.
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