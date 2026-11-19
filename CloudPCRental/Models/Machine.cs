using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudPCRental.Models
{
    [Table("machines")]
    public class Machine
    {
        [Key]
        public int MachineId { get; set; }

        [Required(ErrorMessage = "กรุณากรอกชื่อเครื่อง")]
        [StringLength(100)]
        public string MachineName { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุ CPU")]
        [StringLength(100)]
        public string CPU { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุ RAM")]
        [StringLength(50)]
        public string RAM { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณาระบุ GPU")]
        [StringLength(100)]
        public string GPU { get; set; } = string.Empty;

        [Required(ErrorMessage = "กรุณากรอกราคาต่อชั่วโมง")]
        [Column(TypeName = "decimal(10,2)")]
        public decimal PricePerHour { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Available"; // "Available", "Rented", "Maintenance", "PendingRepair"

        [NotMapped]
        public string? RepairNote { get; set; }

        [NotMapped]
        public string? ReportedBy { get; set; }

        [NotMapped]
        public DateTime? ReportedAt { get; set; }

        [NotMapped]
        public string? ApprovedBy { get; set; }

        [NotMapped]
        public string? ImageUrl { get; set; }

        [NotMapped]
        public string DisplayImageUrl
        {
            get
            {
                if (!string.IsNullOrEmpty(ImageUrl)) return ImageUrl;

                var name = (MachineName ?? "").ToLower();
                var gpu = (GPU ?? "").ToLower();

                if (name.Contains("ai") || name.Contains("data") || gpu.Contains("a100") || name.Contains("server") || name.Contains("เซิร์ฟเวอร์") || name.Contains("dev"))
                    return "/images/machines/pc-ai-server.png";

                if (name.Contains("render") || name.Contains("ตัดต่อ") || name.Contains("studio") || name.Contains("ดีไซเนอร์") || name.Contains("graphic") || name.Contains("workstation"))
                    return "/images/machines/pc-workstation.png";

                if (name.Contains("office") || name.Contains("work") || name.Contains("สำนักงาน") || name.Contains("เริ่มต้น") || name.Contains("ทั่วไป"))
                    return "/images/machines/pc-office.png";

                // ค่าเริ่มต้นเป็นคอมพิวเตอร์เกมมิ่งสเปกแรง
                return "/images/machines/pc-gaming.png";
            }
        }
    }
}

