using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceMVC.Data;

public partial class HoiDap
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int MaHdap { get; set; }

    public int MaYt { get; set; }
    public string CauHoi { get; set; } = null!;

    public string TraLoi { get; set; } = null!;

    public DateTime NgayDua { get; set; }

    public string MaNv { get; set; } = null!;

    public virtual NhanVien MaNvNavigation { get; set; } = null!;
    public virtual YeuThich MaYtNavigation { get; set; } = null!;
}
