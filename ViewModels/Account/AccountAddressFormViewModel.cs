using System.ComponentModel.DataAnnotations;

namespace e_commerce_web_customer.ViewModels.Account;

public sealed class AccountAddressFormViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận.")]
    [MaxLength(255)]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [MaxLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn tỉnh / thành phố.")]
    [MaxLength(30)]
    public string ProvinceCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ProvinceName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn quận / huyện.")]
    [MaxLength(30)]
    public string DistrictCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string DistrictName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn phường / xã.")]
    [MaxLength(30)]
    public string WardCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string WardName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ cụ thể.")]
    [MaxLength(500)]
    public string DetailAddress { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = true;
}
