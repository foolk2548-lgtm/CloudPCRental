using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudPCRental.Models
{
    /// <summary>
    /// โมเดลสำหรับจัดเก็บบันทึกประวัติความปลอดภัยและกิจกรรมต่างๆ ของระบบ (System & Audit Log)
    /// แมปตรงกับตาราง `system_logs` ในฐานข้อมูล MySQL (csi402db)
    /// </summary>
    [Table("system_logs")]
    public class SystemLog
    {
        [Key]
        [Column("LogId")]
        public int LogId { get; set; }

        [Column("Timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [StringLength(100)]
        [Column("PerformedBy")]
        public string PerformedBy { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("Category")]
        public string Category { get; set; } = "System"; // Security, Booking, Hardware, Login, Payment, System

        [Required]
        [Column("Detail", TypeName = "text")]
        public string Detail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column("Severity")]
        public string Severity { get; set; } = "ข้อมูลทั่วไป"; // ข้อมูลทั่วไป, สำคัญ, วิกฤต

        [StringLength(50)]
        [Column("IpAddress")]
        public string IpAddress { get; set; } = "127.0.0.1";
    }
}
