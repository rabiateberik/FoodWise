using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FoodWise.Domain.Common;

namespace FoodWise.Domain.Entities;

public class RecipeIngredient : BaseEntity
{
    public int RecipeId { get; set; }

    public int ProductId { get; set; }

    public int? UnitId { get; set; }

    public decimal? Quantity { get; set; }

    public bool IsRequired { get; set; } = true;

    public Recipe Recipe { get; set; } = null!;

    public Product Product { get; set; } = null!;

    public Unit? Unit { get; set; }
}