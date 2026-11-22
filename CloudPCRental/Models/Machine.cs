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
        public string Category
        {
            get
            {
                var name = (MachineName ?? "").ToLower();
                var gpu = (GPU ?? "").ToLower();

                if (name.Contains("ai") || name.Contains("data") || gpu.Contains("a100") || gpu.Contains("h100") || name.Contains("server") || name.Contains("เซิร์ฟเวอร์") || name.Contains("dev") || name.Contains("cluster") || name.Contains("ปัญญาประดิษฐ์") || name.Contains("ดีพเลิร์นนิง") || MachineId == 3 || MachineId == 6 || MachineId == 9 || MachineId == 13 || MachineId == 17 || MachineId == 21)
                    return "ai";

                if (name.Contains("render") || name.Contains("ตัดต่อ") || name.Contains("studio") || name.Contains("ดีไซเนอร์") || name.Contains("graphic") || name.Contains("workstation") || name.Contains("3d") || name.Contains("วิดีโอ") || name.Contains("โมชัน") || MachineId == 2 || MachineId == 5 || MachineId == 8 || MachineId == 12 || MachineId == 16 || MachineId == 20)
                    return "graphic";

                if (name.Contains("office") || name.Contains("สำนักงาน") || name.Contains("เริ่มต้น") || name.Contains("ทั่วไป") || name.Contains("เอกสาร") || name.Contains("มัลติมีเดีย") || name.Contains("work 04") || name.Contains("work 14") || name.Contains("work 18") || MachineId == 4 || MachineId == 10 || MachineId == 14 || MachineId == 18 || MachineId == 22)
                    return "office";

                return "gaming";
            }
        }

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

