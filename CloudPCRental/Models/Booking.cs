using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CloudPCRental.Models
{
    [Table("bookings")]
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุผู้ใช้งาน")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุเครื่องคอมพิวเตอร์")]
        public int MachineId { get; set; }

        [Required(ErrorMessage = "กรุณาระบุจำนวนเงินรวม")]
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 999999.99, ErrorMessage = "จำนวนเงินต้องไม่ติดลบ")]
        public decimal TotalAmount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; 
        // สถานะที่รองรับ: 
        // - "Pending": รอตรวจสอบสลิป
        // - "Approved": อนุมัติการชำระเงินเรียบร้อย
        // - "Rejected": ปฏิเสธสลิป / สลิปไม่ถูกต้อง
        // - "RefundRequested": ลูกค้าขอยกเลิกและขอคืนเงิน
        // - "Refunded": คืนเงินเรียบร้อยแล้ว (แนบสลิปคืนเงินแล้ว)

        [StringLength(255)]
        public string? SlipImageUrl { get; set; } // URL/พาธ รูปสลิปการโอนเงินของลูกค้า

        [StringLength(255)]
        public string? RefundSlipImageUrl { get; set; } // URL/พาธ รูปสลิปการโอนคืนเงินจากฝ่ายบัญชี

        [StringLength(255)]
        public string? RefundReason { get; set; } // เหตุผลการขอคืนเงิน

        public DateTime? RefundDate { get; set; } // วันที่ฝ่ายบัญชีดำเนินการคืนเงิน

        [StringLength(255)]
        public string? Note { get; set; } // หมายเหตุเพิ่มเติมจากพนักงานบัญชี

        // ชั่วโมงและช่วงเวลาการจอง (สำหรับการคำนวณและอ้างอิง)
        public int RentalHours { get; set; } = 1;

        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        // Navigation Properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("MachineId")]
        public virtual Machine? Machine { get; set; }
    }
}

