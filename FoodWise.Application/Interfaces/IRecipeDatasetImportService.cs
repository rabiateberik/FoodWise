
// Tarif veri setindeki JSON tariflerini veritabanına aktarmak için kullanılan servis sözleşmesidir.

namespace FoodWise.Application.Interfaces;

public interface IRecipeDatasetImportService
{
    // Belirtilen dosya yolundaki JSON tarif verilerini içe aktarır.
    // Eklenen tarif sayısını döndürür.
    Task<int> ImportFromJsonAsync(string filePath);
}

