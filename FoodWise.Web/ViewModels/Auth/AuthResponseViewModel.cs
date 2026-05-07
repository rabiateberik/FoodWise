// Bu ViewModel, FoodWise.API tarafından login veya register işleminden sonra dönen cevabı temsil eder.
// API'den gelen token, kullanıcı bilgileri ve işlem sonucu Web tarafında bu model ile karşılanır.

namespace FoodWise.Web.ViewModels.Auth;

public class AuthResponseViewModel
{
    public bool Success { get; set; }

    public string? Message { get; set; }

    public string? UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Token { get; set; }
}