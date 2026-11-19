using Microsoft.AspNetCore.Mvc;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// ศูนย์รับเรื่องแจ้งปัญหาและระบบตอบแชทลูกค้า (Support Tickets & Live Chat Hub)
    /// </summary>
    public partial class AdminController : Controller
    {
        // ==========================================
        // Support Tickets & Customer Live Chat Hub (Admin / Manager / Staff)
        // ==========================================
        [HttpGet]
        public IActionResult SupportTickets(int? selectedTicketId = null)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงศูนย์สนับสนุนลูกค้า";
                return RedirectToAction("Index", "CloudPC");
            }

            var allTickets = MockDataStore.GetSupportTickets();
            var selectedTicket = selectedTicketId.HasValue 
                ? allTickets.FirstOrDefault(t => t.TicketId == selectedTicketId.Value)
                : allTickets.FirstOrDefault();

            ViewBag.SelectedTicket = selectedTicket;
            ViewBag.AllTickets = allTickets;
            return View(allTickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ReplySupportTicket(int ticketId, string replyContent, string? newStatus = null)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ตอบกลับคำขอ";
                return RedirectToAction("Index", "CloudPC");
            }

            if (string.IsNullOrWhiteSpace(replyContent))
            {
                TempData["ErrorMessage"] = "กรุณากรอกข้อความตอบกลับลูกค้า";
                return RedirectToAction("SupportTickets", new { selectedTicketId = ticketId });
            }

            var existingTicket = MockDataStore.GetSupportTicketById(ticketId);
            if (existingTicket != null && existingTicket.Status == "Resolved")
            {
                TempData["ErrorMessage"] = "เคสนี้ถูกปิดแล้ว ไม่สามารถส่งข้อความเพิ่มได้ (รอลูกค้าส่งข้อความใหม่เข้ามา)";
                return RedirectToAction("SupportTickets", new { selectedTicketId = ticketId });
            }

            int staffUserId = HttpContext.Session.GetInt32("UserId") ?? 1;
            string staffUsername = HttpContext.Session.GetString("Username") ?? "เจ้าหน้าที่";
            string staffRole = HttpContext.Session.GetString("Role") ?? "Staff";

            // บันทึกชื่อจริงและตำแหน่งจริงของผู้ตอบ (Manager, Admin, Staff, Accountant)
            MockDataStore.AddMessageToTicket(ticketId, staffUserId, staffUsername, staffRole, replyContent.Trim(), isAdmin: true);

            if (!string.IsNullOrEmpty(newStatus))
            {
                MockDataStore.UpdateSupportTicketStatus(ticketId, newStatus);
            }

            TempData["SuccessMessage"] = "ส่งข้อความตอบกลับลูกค้าเรียบร้อยแล้ว";
            return RedirectToAction("SupportTickets", new { selectedTicketId = ticketId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResolveSupportTicket(int ticketId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์จัดการสถานะ";
                return RedirectToAction("Index", "CloudPC");
            }

            MockDataStore.UpdateSupportTicketStatus(ticketId, "Resolved");
            TempData["SuccessMessage"] = $"ปิดเคสและทำเครื่องหมายว่าแก้ไขปัญหาเรียบร้อยแล้ว (#TK-{ticketId})";
            return RedirectToAction("SupportTickets", new { selectedTicketId = ticketId });
        }
    }
}
