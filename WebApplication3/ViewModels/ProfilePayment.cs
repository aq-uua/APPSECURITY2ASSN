using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class ProfilePayment
{
    [Display(Name = "Credit Card Number")]
    [RegularExpression(@"^\d{13,19}$", ErrorMessage = "Credit card must be 13 to 19 digits.")]
    public string? CreditCard { get; set; }

    public string? MaskedCard { get; set; }
}
