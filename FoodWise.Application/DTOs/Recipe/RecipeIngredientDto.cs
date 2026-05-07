using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Tarifin içinde bulunan malzemeleri API cevabında göstermek için kullanılır.
namespace FoodWise.Application.DTOs.Recipe;

public class RecipeIngredientDto
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public decimal? Quantity { get; set; }

    public string? UnitName { get; set; }

    public bool IsRequired { get; set; }
}