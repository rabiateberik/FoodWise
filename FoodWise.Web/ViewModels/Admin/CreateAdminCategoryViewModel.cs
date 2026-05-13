// CreateAdminCategoryViewModel, admin panelinden yeni kategori eklemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Admin;

public class CreateAdminCategoryViewModel
{
    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}