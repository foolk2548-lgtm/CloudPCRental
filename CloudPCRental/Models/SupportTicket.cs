using System;
using System.Collections.Generic;

namespace CloudPCRental.Models
{
    public class SupportMessage
    {
        public int MessageId { get; set; }
        public int TicketId { get; set; }
        public int SenderUserId { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string SenderRole { get; set; } = "Customer"; // "Customer" หรือ "Admin"
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public bool IsAdminReply { get; set; } = false;
    }

    public class SupportTicket
    {
        public int TicketId { get; set; }
        public string TicketCode { get; set; } = string.Empty; // เช่น "#TK-1001"
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Category { get; set; } = "แจ้งปัญหาการใช้งาน"; // "การเชื่อมต่อ & รีโมท", "การจอง & ชำระเงิน", "ซอฟต์แวร์ & สเปกเครื่อง", "ทั่วไป"
        public int? RelatedBookingId { get; set; }
        public string? RelatedMachineName { get; set; }
        public string Status { get; set; } = "WaitingAdmin"; // "WaitingAdmin" (รอแอดมินตอบ), "Answered" (แอดมินตอบแล้ว), "Resolved" (แก้ไขแล้ว)
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public List<SupportMessage> Messages { get; set; } = new();
    }

    public class ChatMessageRequest
    {
        public int? TicketId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Subject { get; set; }
        public string? Category { get; set; }
        public int? BookingId { get; set; }
        public string? MachineName { get; set; }
    }
}
