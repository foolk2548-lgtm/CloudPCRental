using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudPCRental.Models
{
    /// <summary>
    /// โมเดลสำหรับจัดเก็บบันทึกประวัติความปลอดภัยและกิจกรรมต่างๆ ของระบบ (System & Audit Log)
    /// </summary>
    [Table("system_logs")]
    public class SystemLog
    {
        [Key]
        public int LogId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        public string PerformedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "System"; // Security, Booking, Hardware, Login, Payment, System

        [Required]
        [StringLength(255)]
        public string Detail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Severity { get; set; } = "ข้อมูลทั่วไป"; // ข้อมูลทั่วไป, สำคัญ, วิกฤต

        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";
    }
}
