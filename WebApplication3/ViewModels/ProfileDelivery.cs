using System.ComponentModel.DataAnnotations;

namespace WebApplication3.ViewModels;

public class ProfileDelivery
{
    [StringLength(500)]
    [Display(Name = "Delivery Address")]
    public string? DeliveryAddress { get; set; }
}
