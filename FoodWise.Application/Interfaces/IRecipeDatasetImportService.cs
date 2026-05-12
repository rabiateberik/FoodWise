using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Tarif veri setindeki JSON tariflerini veritabanına aktarmak için kullanılan servis sözleşmesidir.

namespace FoodWise.Application.Interfaces;

public interface IRecipeDatasetImportService
{
    Task<int> ImportFromJsonAsync(string filePath);
}