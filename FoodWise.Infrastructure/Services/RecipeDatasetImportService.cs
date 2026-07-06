
// RecipeDatasetImportService, Türkçe tarif veri setindeki JSON verilerini Recipe tablosuna aktarır.
// Dataset içindeki malzemeler metinsel olarak saklanır ve öneri algoritması için normalize edilir.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FoodWise.Application.Interfaces;
using FoodWise.Domain.Entities;
using FoodWise.Domain.Enums;
using FoodWise.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodWise.Infrastructure.Services;

public class RecipeDatasetImportService : IRecipeDatasetImportService
{
    private readonly FoodWiseDbContext _context;

    public RecipeDatasetImportService(FoodWiseDbContext context)
    {
        _context = context;
    }

    // JSON dosyasındaki tarifleri okuyarak veritabanındaki Recipe tablosuna aktarır.
    public async Task<int> ImportFromJsonAsync(string filePath)
    {
        // Dosya bulunamazsa aktarım yapılmaz.
        if (!File.Exists(filePath))
            return 0;

        await using var stream = File.OpenRead(filePath);

        // JSON dosyasındaki tarifler RecipeDatasetItem listesine çevrilir.
        var datasetRecipes = await JsonSerializer.DeserializeAsync<List<RecipeDatasetItem>>(
            stream,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (datasetRecipes == null || !datasetRecipes.Any())
            return 0;

        // Daha önce eklenmiş tariflerin ExternalApiId değerleri alınır.
        // Böylece aynı tarif tekrar veritabanına eklenmez.
        var existingExternalIds = await _context.Recipes
            .Where(x => x.ExternalApiId != null)
            .Select(x => x.ExternalApiId!)
            .ToListAsync();

        var existingExternalIdSet = existingExternalIds.ToHashSet();

        var recipesToAdd = new List<Recipe>();

        foreach (var item in datasetRecipes)
        {
            // Tarif adı yoksa kayıt geçersiz kabul edilir.
            if (string.IsNullOrWhiteSpace(item.TarifAdi))
                continue;

            var externalApiId = CreateExternalApiId(item);

            // Aynı tarif daha önce eklenmişse tekrar eklenmez.
            if (existingExternalIdSet.Contains(externalApiId))
                continue;

            var ingredientsText = CreateIngredientsText(item.Malzemeler);
            var normalizedIngredientsText = CreateNormalizedIngredientsText(item.Malzemeler);
            var instructions = CreateInstructionsText(item.YapilisAdimlari);

            // Malzeme veya yapılış adımı olmayan tarifler öneri sistemi için uygun değildir.
            if (string.IsNullOrWhiteSpace(ingredientsText) || string.IsNullOrWhiteSpace(instructions))
                continue;

            // Dataset verisi sistemde kullanılan Recipe entity yapısına dönüştürülür.
            var recipe = new Recipe
            {
                Name = item.TarifAdi.Trim(),
                Description = CreateDescription(item),
                Instructions = instructions,
                PreparationTimeMinutes = CalculateTotalPreparationTime(
                    item.HazirlikSuresiDk,
                    item.PisirmeSuresiDk),
                ImageUrl = null,
                SourceType = RecipeSourceType.Local,
                ExternalApiId = externalApiId,
                IngredientsText = ingredientsText,
                NormalizedIngredientsText = normalizedIngredientsText,
                SourceUrl = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            recipesToAdd.Add(recipe);
            existingExternalIdSet.Add(externalApiId);
        }

        if (!recipesToAdd.Any())
            return 0;

        await _context.Recipes.AddRangeAsync(recipesToAdd);
        await _context.SaveChangesAsync();

        return recipesToAdd.Count;
    }

    // Tarif için benzersiz bir dış kaynak Id değeri üretir.
    // Öncelik olarak content hash, sonra dataset index bilgisi kullanılır.
    private static string CreateExternalApiId(RecipeDatasetItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.Source?.ContentHash))
            return $"turkish-recipe-{item.Source.ContentHash}";

        if (item.Source?.Index != null)
            return $"turkish-recipe-{item.Source.Index}";

        return $"turkish-recipe-{NormalizeText(item.TarifAdi ?? Guid.NewGuid().ToString())}";
    }

    // Tarif açıklaması için kategori, zorluk ve pişirme yöntemi bilgilerini birleştirir.
    private static string CreateDescription(RecipeDatasetItem item)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(item.Kategori))
            parts.Add($"Kategori: {item.Kategori}");

        if (!string.IsNullOrWhiteSpace(item.Zorluk))
            parts.Add($"Zorluk: {item.Zorluk}");

        if (item.PisirmeYontemi != null && item.PisirmeYontemi.Any())
            parts.Add($"Pişirme yöntemi: {string.Join(", ", item.PisirmeYontemi)}");

        return parts.Any()
            ? string.Join(" | ", parts)
            : "Türkçe tarif veri setinden aktarılmıştır.";
    }

    // Hazırlık ve pişirme sürelerini toplayarak toplam süreyi hesaplar.
    // Süre bilgisi yoksa varsayılan olarak 30 dakika kullanılır.
    private static int CalculateTotalPreparationTime(JsonElement? preparationMinute, JsonElement? cookingMinute)
    {
        var preparation = GetNumberValue(preparationMinute);
        var cooking = GetNumberValue(cookingMinute);

        var total = preparation + cooking;

        return total > 0 ? Convert.ToInt32(Math.Round(total)) : 30;
    }

    // JSON içindeki sayı veya metin olarak gelen süre/miktar değerlerini sayıya çevirir.
    private static double GetNumberValue(JsonElement? element)
    {
        if (element == null)
            return 0;

        var value = element.Value;

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetDouble(out var number))
                return number;

            return 0;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();

            if (string.IsNullOrWhiteSpace(text))
                return 0;

            text = text.Replace(",", ".");

            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                return number;
        }

        return 0;
    }

    // Malzeme listesini kullanıcıya gösterilecek metin formatına çevirir.
    private static string CreateIngredientsText(List<RecipeDatasetIngredient>? ingredients)
    {
        if (ingredients == null || !ingredients.Any())
            return string.Empty;

        var ingredientLines = ingredients
            .Where(x => !string.IsNullOrWhiteSpace(x.Isim))
            .Select(FormatIngredient)
            .ToList();

        return string.Join(Environment.NewLine, ingredientLines);
    }

    // Tek bir malzemeyi miktar, birim ve isim bilgileriyle okunabilir hale getirir.
    private static string FormatIngredient(RecipeDatasetIngredient ingredient)
    {
        var amountText = FormatAmount(ingredient.Miktar);

        var unitText = !string.IsNullOrWhiteSpace(ingredient.Birim)
            ? ingredient.Birim.Trim()
            : string.Empty;

        var name = ingredient.Isim?.Trim() ?? string.Empty;

        // Miktar metninin içinde birim zaten geçiyorsa aynı birim tekrar yazılmaz.
        if (!string.IsNullOrWhiteSpace(amountText) &&
            !string.IsNullOrWhiteSpace(unitText) &&
            amountText.Contains(unitText, StringComparison.CurrentCultureIgnoreCase))
        {
            unitText = string.Empty;
        }

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(amountText))
            parts.Add(amountText);

        if (!string.IsNullOrWhiteSpace(unitText))
            parts.Add(unitText);

        if (!string.IsNullOrWhiteSpace(name))
            parts.Add(name);

        return string.Join(" ", parts);
    }

    // JSON içindeki miktar bilgisini metne çevirir.
    // Miktar sayı veya metin olarak gelebileceği için iki durum da kontrol edilir.
    private static string FormatAmount(JsonElement? amountElement)
    {
        if (amountElement == null)
            return string.Empty;

        var value = amountElement.Value;

        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return string.Empty;

        if (value.ValueKind == JsonValueKind.Number)
        {
            if (value.TryGetDecimal(out var decimalValue))
                return FormatDecimal(decimalValue);

            return string.Empty;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();

            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim();
        }

        return string.Empty;
    }

    // Tam sayı miktarları küsüratsız, ondalıklı miktarları kısa formatta yazar.
    private static string FormatDecimal(decimal value)
    {
        return value % 1 == 0
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    // Malzeme isimlerini tarif öneri algoritmasında kullanılacak sadeleştirilmiş metne çevirir.
    private static string CreateNormalizedIngredientsText(List<RecipeDatasetIngredient>? ingredients)
    {
        if (ingredients == null || !ingredients.Any())
            return string.Empty;

        var normalizedIngredients = ingredients
            .Where(x => !string.IsNullOrWhiteSpace(x.Isim))
            .Select(x => NormalizeText(x.Isim!))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        return string.Join(" ", normalizedIngredients);
    }

    // Tarifin yapılış adımlarını tek metin alanı olarak birleştirir.
    private static string CreateInstructionsText(List<string>? steps)
    {
        if (steps == null || !steps.Any())
            return string.Empty;

        return string.Join(
            Environment.NewLine,
            steps.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    // Türkçe karakterleri sadeleştirir ve öneri algoritması için malzeme metnini normalize eder.
    private static string NormalizeText(string input)
    {
        var text = input.Trim().ToLower(new CultureInfo("tr-TR"));

        text = text
            .Replace("ç", "c")
            .Replace("ğ", "g")
            .Replace("ı", "i")
            .Replace("ö", "o")
            .Replace("ş", "s")
            .Replace("ü", "u");

        text = Regex.Replace(text, @"[^a-z0-9\s]", " ");

        // Malzeme eşleştirmesini bozabilecek ölçü, boyut ve belirsiz ifadeler temizlenir.
        text = Regex.Replace(
            text,
            @"\b(orta|buyuk|kucuk|boy|taze|ince|kalin|yagli|yagsiz|dolu|dolusu|yaklasik|biraz|bir|yarim)\b",
            " ");

        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    // JSON tarif verisindeki ana tarif alanlarını temsil eder.
    private class RecipeDatasetItem
    {
        [JsonPropertyName("tarif_adi")]
        public string? TarifAdi { get; set; }

        [JsonPropertyName("kategori")]
        public string? Kategori { get; set; }

        [JsonPropertyName("porsiyon")]
        public JsonElement? Porsiyon { get; set; }

        [JsonPropertyName("hazirlik_suresi_dk")]
        public JsonElement? HazirlikSuresiDk { get; set; }

        [JsonPropertyName("pisirme_suresi_dk")]
        public JsonElement? PisirmeSuresiDk { get; set; }

        [JsonPropertyName("zorluk")]
        public string? Zorluk { get; set; }

        [JsonPropertyName("pisirme_yontemi")]
        public List<string>? PisirmeYontemi { get; set; }

        [JsonPropertyName("malzemeler")]
        public List<RecipeDatasetIngredient>? Malzemeler { get; set; }

        [JsonPropertyName("yapilis_adimlari")]
        public List<string>? YapilisAdimlari { get; set; }

        [JsonPropertyName("_source")]
        public RecipeDatasetSource? Source { get; set; }
    }

    // JSON tarif verisindeki malzeme alanlarını temsil eder.
    private class RecipeDatasetIngredient
    {
        [JsonPropertyName("isim")]
        public string? Isim { get; set; }

        [JsonPropertyName("miktar")]
        public JsonElement? Miktar { get; set; }

        [JsonPropertyName("birim")]
        public string? Birim { get; set; }
    }

    // Dataset içindeki kaynak ve takip bilgilerini temsil eder.
    private class RecipeDatasetSource
    {
        [JsonPropertyName("raw_name")]
        public string? RawName { get; set; }

        [JsonPropertyName("index")]
        public int? Index { get; set; }

        [JsonPropertyName("content_hash")]
        public string? ContentHash { get; set; }
    }
}

