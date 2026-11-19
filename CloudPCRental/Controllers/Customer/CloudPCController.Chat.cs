using Microsoft.AspNetCore.Mvc;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// โมดูลระบบแชทแบบเรียลไทม์และการแจ้งปัญหาการใช้งานสำหรับลูกค้า (Customer Live Chat & Support Ticket)
    /// </summary>
    public partial class CloudPCController : Controller
    {
        // ==========================================
        // Live Chat & Support Ticket Endpoints (Customer)
        // ==========================================
        [HttpGet]
        public IActionResult GetChatData(int? ticketId = null)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            if (!currentUserId.HasValue)
            {
                return Json(new { success = false, requireLogin = true, message = "กรุณาเข้าสู่ระบบก่อนใช้งานแชท" });
            }

            var tickets = MockDataStore.GetSupportTickets(currentUserId.Value);
            
            SupportTicket? activeTicket = null;
            if (ticketId.HasValue)
            {
                activeTicket = tickets.FirstOrDefault(t => t.TicketId == ticketId.Value);
            }
            if (activeTicket == null)
            {
                activeTicket = tickets.FirstOrDefault(t => t.Status != "Resolved") ?? tickets.FirstOrDefault();
            }

            return Json(new
            {
                success = true,
                requireLogin = false,
                currentUserId = currentUserId.Value,
                tickets = tickets.Select(t => new
                {
                    ticketId = t.TicketId,
                    ticketCode = t.TicketCode,
                    subject = t.Subject,
                    status = t.Status,
                    updatedAt = t.UpdatedAt.ToString("HH:mm น. (dd/MM)"),
                    unreadCount = t.Messages.Count(m => m.IsAdminReply)
                }),
                activeTicket = activeTicket == null ? null : new
                {
                    ticketId = activeTicket.TicketId,
                    ticketCode = activeTicket.TicketCode,
                    subject = activeTicket.Subject,
                    category = activeTicket.Category,
                    status = activeTicket.Status,
                    relatedMachine = activeTicket.RelatedMachineName,
                    messages = activeTicket.Messages.Select(m => new
                    {
                        messageId = m.MessageId,
                        senderName = m.SenderName,
                        senderRole = m.SenderRole,
                        content = m.Content,
                        sentAt = m.SentAt.ToString("HH:mm น."),
                        isAdmin = m.IsAdminReply
                    })
                }
            });
        }

        [HttpPost]
        public IActionResult SendChatMessage([FromBody] ChatMessageRequest request)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            string? currentUsername = HttpContext.Session.GetString("Username");
            if (!currentUserId.HasValue || string.IsNullOrEmpty(currentUsername))
            {
                return Json(new { success = false, requireLogin = true, message = "กรุณาเข้าสู่ระบบก่อนส่งข้อความ" });
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { success = false, message = "ข้อความไม่สามารถเว้นว่างได้" });
            }

            var currentUserObj = MockDataStore.GetUsers().FirstOrDefault(u => u.UserId == currentUserId.Value);
            string userEmail = !string.IsNullOrEmpty(currentUserObj?.Email) 
                ? currentUserObj.Email 
                : (currentUsername.Contains("@") ? currentUsername : $"{currentUsername.ToLower()}@example.com");

            SupportTicket ticket;
            if (request.TicketId.HasValue && request.TicketId.Value > 0)
            {
                var existing = MockDataStore.GetSupportTicketById(request.TicketId.Value);
                // ตรวจสอบอย่างเข้มงวดว่า Ticket นี้เป็นของ currentUserId จริงหรือไม่ เพื่อป้องกันแชทปนกันข้ามบัญชี
                if (existing != null && existing.UserId == currentUserId.Value)
                {
                    ticket = existing;
                }
                else
                {
                    // หากไม่ใช่ของตนเอง ให้ค้นหาห้องแชทของตัวเองหรือเปิดห้องใหม่
                    var userTickets = MockDataStore.GetSupportTickets(currentUserId.Value);
                    ticket = userTickets.FirstOrDefault(t => t.Status != "Resolved")
                             ?? MockDataStore.CreateSupportTicket(currentUserId.Value, currentUsername, userEmail, request.Subject ?? "แจ้งปัญหาการใช้งานทั่วไป", request.Category ?? "ทั่วไป", request.Message, request.BookingId, request.MachineName);
                }
            }
            else
            {
                // หากไม่มี TicketId ให้ค้นหาห้องแชทเดิมที่ยังไม่ปิดของตนเองก่อน ถ้าไม่มีจึงสร้างใหม่
                var userTickets = MockDataStore.GetSupportTickets(currentUserId.Value);
                ticket = userTickets.FirstOrDefault(t => t.Status != "Resolved")
                         ?? MockDataStore.CreateSupportTicket(currentUserId.Value, currentUsername, userEmail, request.Subject ?? "แจ้งปัญหาการใช้งานทั่วไป", request.Category ?? "ทั่วไป", request.Message, request.BookingId, request.MachineName);
            }

            SupportMessage? msg = null;
            if (ticket.Messages.All(m => m.Content != request.Message))
            {
                msg = MockDataStore.AddMessageToTicket(ticket.TicketId, currentUserId.Value, currentUsername, "Customer", request.Message, isAdmin: false);
            }
            else
            {
                msg = ticket.Messages.LastOrDefault();
            }

            return Json(new
            {
                success = true,
                ticketId = ticket.TicketId,
                message = msg != null ? new
                {
                    messageId = msg.MessageId,
                    senderName = msg.SenderName,
                    senderRole = msg.SenderRole,
                    content = msg.Content,
                    sentAt = msg.SentAt.ToString("HH:mm น."),
                    isAdmin = msg.IsAdminReply
                } : null
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSupportTicket(string subject, string category, string message, int? bookingId = null, string? machineName = null)
        {
            int? currentUserId = HttpContext.Session.GetInt32("UserId");
            string? currentUsername = HttpContext.Session.GetString("Username");
            if (!currentUserId.HasValue || string.IsNullOrEmpty(currentUsername))
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนส่งข้อความแจ้งปัญหา";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Help", "CloudPC") });
            }

            var currentUserObj = MockDataStore.GetUsers().FirstOrDefault(u => u.UserId == currentUserId.Value);
            string email = !string.IsNullOrEmpty(currentUserObj?.Email) 
                ? currentUserObj.Email 
                : (currentUsername.Contains("@") ? currentUsername : $"{currentUsername.ToLower()}@example.com");
            var ticket = MockDataStore.CreateSupportTicket(currentUserId.Value, currentUsername, email, subject, category, message, bookingId, machineName);

            TempData["SuccessMessage"] = $"แจ้งปัญหาเรียบร้อยแล้ว (รหัสคำขอ {ticket.TicketCode}) ทีมงานผู้ดูแลระบบได้รับข้อความและจะตอบกลับทางแชทโดยเร็วครับ";
            return RedirectToAction("Help");
        }
    }
}
