// UpdateAdminCategoryViewModel, admin panelinden kategori güncellemek için kullanılır.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Admin;

public class UpdateAdminCategoryViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Kategori adı zorunludur.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}