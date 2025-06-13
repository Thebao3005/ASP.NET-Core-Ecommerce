using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerceMVC.Data;

public partial class NhaCungCap
{
    [Key]
    [Required]
    [MaxLength(10)]
    public string MaNcc { get; set; } = null!;

    [Required]
    public string TenCongTy { get; set; } = null!;

    public string Logo { get; set; } = null!;

    public string? NguoiLienLac { get; set; }

    public string Email { get; set; } = null!;

    public string? DienThoai { get; set; }

    public string? DiaChi { get; set; }

    public string? MoTa { get; set; }

    public virtual ICollection<HangHoa> HangHoas { get; set; } = new List<HangHoa>();
}
