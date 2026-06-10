// Bu ViewModel, kullanıcının stokundaki bir ürünü paylaşım ilanına dönüştürmesi için kullanılır.
// Formdan alınan bilgiler Sharing API'ye gönderilir.
// Teslim noktaları kullanıcının kayıtlı konumuna göre yakınlık bilgisiyle birlikte gösterilir.

using System.ComponentModel.DataAnnotations;

namespace FoodWise.Web.ViewModels.Sharing;

public class CreateShareListingViewModel
{
    [Required]
    public int StockItemId { get; set; }

    public string? ProductName { get; set; }

    [Required(ErrorMessage = "Teslim noktası seçimi zorunludur.")]
    [Display(Name = "Teslim Noktası")]
    public int DeliveryPointId { get; set; }

    [Required(ErrorMessage = "İlan başlığı zorunludur.")]
    [StringLength(150, ErrorMessage = "Başlık en fazla 150 karakter olabilir.")]
    [Display(Name = "İlan Başlığı")]
    public string Title { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Miktar alanı zorunludur.")]
    [Range(0.01, 9999, ErrorMessage = "Miktar 0'dan büyük olmalıdır.")]
    [Display(Name = "Paylaşılacak Miktar")]
    public decimal Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Teslim başlangıç zamanı zorunludur.")]
    [Display(Name = "Teslim Başlangıç Zamanı")]
    public DateTime PickupStartTime { get; set; } = DateTime.Now.AddHours(1);

    [Required(ErrorMessage = "Teslim bitiş zamanı zorunludur.")]
    [Display(Name = "Teslim Bitiş Zamanı")]
    public DateTime PickupEndTime { get; set; } = DateTime.Now.AddHours(24);

    // Web arayüzünde teslim noktalarını sadece klasik dropdown olarak değil,
    // konum yakınlığı, çalışma saati ve saklama tipiyle birlikte göstermek için kullanılır.
    public List<DeliveryPointViewModel> DeliveryPoints { get; set; } = new();

    // Kullanıcı web arayüzünde teslim noktası araması yaptığında form state'ini korumak için kullanılır.
    public string? DeliveryPointSearch { get; set; }
}