using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// โมดูลระบบกระเป๋าเงิน (CloudPC Wallet), การเติมเงิน, หักเครดิต, คำขอคืนเงิน และ Wallet AJAX APIs
    /// </summary>
    public partial class CloudPCController : Controller
    {
        // ==========================================
        // 8.1 แดชบอร์ดลูกค้า: กระเป๋าเงินของฉัน (Wallet)
        // ==========================================
        [HttpGet]
        public IActionResult Wallet()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อเข้าถึงกระเป๋าเงินของคุณ";
                return RedirectToAction("Login", new { returnUrl = Url.Action("Wallet", "CloudPC") });
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            decimal balance = MockDataStore.GetUserBalance(userId);
            HttpContext.Session.SetString("WalletBalance", balance.ToString("F2"));

            ViewBag.Balance = balance;
            ViewBag.Username = HttpContext.Session.GetString("Username") ?? "ธนพล ดีสมเกียรติ";

            var transactions = MockDataStore.GetUserTransactions(userId);
            return View(transactions);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopUpWallet(decimal amount, string? paymentMethod, IFormFile? slipFile = null, string? sampleSlipUrl = null)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            if (amount <= 0) amount = 100;
            var method = paymentMethod ?? "พร้อมเพย์ QR Code";

            // 1. กรณีเป็นพร้อมเพย์ QR Code: ต้องแนบสลิปและส่งให้ฝ่ายบัญชีตรวจสอบก่อน เครดิตจึงจะเข้ากระเป๋า
            if (method == "พร้อมเพย์ QR Code")
            {
                string? slipUrl = null;

                if (slipFile != null && slipFile.Length > 0)
                {
                    try
                    {
                        string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "slips");
                        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                        string fileName = $"slip_topup_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..6]}{Path.GetExtension(slipFile.FileName)}";
                        string fullPath = Path.Combine(uploadsDir, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await slipFile.CopyToAsync(stream);
                        }
                        slipUrl = $"/uploads/slips/{fileName}";
                    }
                    catch
                    {
                        slipUrl = !string.IsNullOrWhiteSpace(sampleSlipUrl) ? sampleSlipUrl : null;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(sampleSlipUrl))
                {
                    slipUrl = sampleSlipUrl;
                }

                if (string.IsNullOrEmpty(slipUrl))
                {
                    TempData["ErrorMessage"] = "กรุณาแนบไฟล์รูปภาพสลิปหลักฐานการโอนเงินก่อนส่งให้ฝ่ายบัญชีตรวจสอบครับ";
                    return RedirectToAction("Wallet");
                }

                // สร้างคำขอเติมเงินใน MockDataStore รอฝ่ายบัญชีตรวจสอบ
                var topupReq = MockDataStore.CreateTopUpRequest(userId, amount, slipUrl);

                try
                {
                    if (await _context.Database.CanConnectAsync())
                    {
                        var firstMachine = await _context.Machines.FirstOrDefaultAsync();
                        var dbBooking = new Booking
                        {
                            UserId = userId,
                            MachineId = firstMachine?.MachineId ?? 1,
                            RentalHours = 0,
                            TotalAmount = amount,
                            PaymentDate = DateTime.Now,
                            PaymentStatus = "Pending",
                            SlipImageUrl = slipUrl,
                            StartTime = DateTime.Now,
                            EndTime = DateTime.Now,
                            Note = "เติมเงินเข้ากระเป๋าเงิน CloudPC Wallet (รอฝ่ายบัญชีตรวจสอบสลิป)"
                        };
                        _context.Bookings.Add(dbBooking);
                        await _context.SaveChangesAsync();
                    }
                }
                catch { }

                TempData["SuccessMessage"] = $"แจ้งโอนเงินเติมกระเป๋าเงินจำนวน +{amount:N2} THB (สลิป #{topupReq.BookingId}) เรียบร้อยแล้ว! ข้อมูลส่งถึงฝ่ายบัญชีแล้ว เมื่อฝ่ายบัญชีตรวจสอบและกดอนุมัติ ยอดเงินจะเข้ากระเป๋าเงินทันทีครับ";
                return RedirectToAction("Wallet");
            }
            else
            {
                // กรณีบัตรเครดิต/เดบิต (ตัดเงินอัตโนมัติทันที)
                decimal newBalance = MockDataStore.TopUpBalance(userId, amount, method);
                HttpContext.Session.SetString("WalletBalance", newBalance.ToString("F2"));
                TempData["SuccessMessage"] = $"เติมเงินเข้ากระเป๋าเงิน +{amount:N2} THB ผ่าน {method} สำเร็จเรียบร้อยแล้ว! (ยอดเงินคงเหลือปัจจุบัน: {newBalance:N2} THB)";
                return RedirectToAction("Wallet");
            }
        }

        // Action สำหรับทดสอบหักเครดิตกระเป๋าเงินด่วน (Quick Real-time Deduction Test)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeductWallet(decimal amount, string? title)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            decimal currentBal = MockDataStore.GetUserBalance(userId);

            if (currentBal < amount)
            {
                TempData["ErrorMessage"] = $"ยอดเงินคงเหลือไม่พอ (มี {currentBal:N2} THB ขาดอีก {(amount - currentBal):N2} THB) กรุณาเติมเงินก่อนทำรายการ";
                return RedirectToAction("Wallet");
            }

            MockDataStore.DeductBalance(userId, amount, title ?? "ทดสอบหักเครดิตใช้งานระบบ", $"#TEST-{DateTime.Now:HHmmss}");
            decimal newBal = MockDataStore.GetUserBalance(userId);
            HttpContext.Session.SetString("WalletBalance", newBal.ToString("F2"));

            TempData["SuccessMessage"] = $"หักเครดิต -{amount:N2} THB สำเร็จ! ยอดเงินคงเหลือใหม่: {newBal:N2} THB";
            return RedirectToAction("Wallet");
        }

        // Action สำหรับยื่นคำขอยกเลิกการจองและขอคืนเงินส่งถึงฝ่ายบัญชี
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRefund(int bookingId, string refundReason, string? issueDetail, decimal refundAmount = 60.00m)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            string fullReason = string.IsNullOrWhiteSpace(issueDetail) 
                ? refundReason 
                : $"[{refundReason}] {issueDetail}";

            // 1. บันทึกคำขอคืนเงินใน MockDataStore
            MockDataStore.RequestRefundBooking(bookingId, fullReason, userId);
            if (bookingId == 94812 || bookingId == 103 || bookingId == 104)
            {
                MockDataStore.RequestRefundBooking(94812, fullReason, userId);
                MockDataStore.RequestRefundBooking(103, fullReason, userId);
                MockDataStore.RequestRefundBooking(104, fullReason, userId);
            }

            // 2. บันทึกคำขอคืนเงินใน MySQL Database (หากเชื่อมต่ออยู่)
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var b = await _context.Bookings.FirstOrDefaultAsync(x => x.BookingId == bookingId);
                    if (b != null)
                    {
                        b.PaymentStatus = "RefundRequested";
                        b.RefundReason = fullReason;
                        b.RefundDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"ยื่นคำขอยกเลิกและขอคืนเงินรายการ #CPC-{bookingId} เรียบร้อยแล้ว! ข้อมูลถูกส่งไปยังฝ่ายบัญชีเพื่อตรวจสอบเงื่อนไขและอนุมัติเงินคืนเข้ากระเป๋าเงินของคุณ";
            return RedirectToAction("MyBookings");
        }

        // Action สำหรับยกเลิกการจองและคืนเงินเข้ากระเป๋าเงินเรียลไทม์ (โหมดรวดเร็ว)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CancelBooking(decimal refundAmount = 60.00m, string? bookingCode = "#CPC-94812")
        {
            if (!IsLoggedIn()) return RedirectToAction("Login");

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            decimal newBal = MockDataStore.RefundBalance(userId, refundAmount, $"ยกเลิกการจองและคืนเงิน {bookingCode}", bookingCode ?? "#REFUND");
            int targetBookingId = 94812;
            if (!string.IsNullOrWhiteSpace(bookingCode))
            {
                var cleanCode = bookingCode.Replace("#CPC-", "").Replace("#cpc-", "").Replace("#", "").Trim();
                if (int.TryParse(cleanCode, out int parsedId))
                {
                    targetBookingId = parsedId;
                }
            }

            MockDataStore.ApproveRefundBooking(targetBookingId, "ลูกค้ายกเลิกการจองและรับเงินคืนเข้ากระเป๋าเงินทันที");
            MockDataStore.ApproveRefundBooking(94812, "ลูกค้ายกเลิกการจองและรับเงินคืนเข้ากระเป๋าเงินทันที");
            MockDataStore.ApproveRefundBooking(104, "ลูกค้ายกเลิกการจองและรับเงินคืนเข้ากระเป๋าเงินทันที");
            HttpContext.Session.SetString("WalletBalance", newBal.ToString("F2"));

            TempData["SuccessMessage"] = $"ยกเลิกการจอง {bookingCode} สำเร็จ! คืนเงินจำนวน +{refundAmount:N2} THB เข้ากระเป๋าเงินเรียบร้อยแล้ว (ยอดคงเหลือปัจจุบัน: {newBal:N2} THB)";
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string username = HttpContext.Session.GetString("Username") ?? "ลูกค้า";
            MockDataStore.LogActivity(username, "Payment", $"ยกเลิกการจอง {bookingCode} และรับเงินคืน {refundAmount:N2} บาทเข้ากระเป๋าเงิน", "สำคัญ", clientIp);
            return RedirectToAction("MyBookings");
        }

        // ==========================================
        // Real-Time Wallet API Endpoints (AJAX)
        // ==========================================
        [HttpGet]
        public IActionResult GetWalletBalance()
        {
            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            decimal bal = MockDataStore.GetUserBalance(userId);
            var txns = MockDataStore.GetUserTransactions(userId);
            bool hasPending = txns.Any(t => t.Status == "รอตรวจสอบ");
            return Json(new { success = true, balance = bal, formatted = bal.ToString("N0") + " THB", formatted2 = bal.ToString("N2") + " THB", hasPending = hasPending });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TopUpWalletAjax(decimal amount, string? paymentMethod, string? sampleSlipUrl = null)
        {
            if (!IsLoggedIn()) return Json(new { success = false, message = "กรุณาเข้าสู่ระบบก่อน" });
            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            if (amount <= 0) amount = 100;
            var method = paymentMethod ?? "พร้อมเพย์ QR Code";

            if (method == "พร้อมเพย์ QR Code")
            {
                string slipUrl = !string.IsNullOrWhiteSpace(sampleSlipUrl) ? sampleSlipUrl : "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=600&auto=format&fit=crop&q=80";
                var topupReq = MockDataStore.CreateTopUpRequest(userId, amount, slipUrl);
                return Json(new { 
                    success = true, 
                    isPending = true,
                    amount,
                    message = $"ส่งสลิปเติมเงิน +{amount:N2} THB (สลิป #{topupReq.BookingId}) ให้ฝ่ายบัญชีตรวจสอบเรียบร้อยแล้ว! เมื่อฝ่ายบัญชีกดอนุมัติ ยอดเงินจะเข้ากระเป๋าเงินทันที" 
                });
            }
            else
            {
                decimal newBalance = MockDataStore.TopUpBalance(userId, amount, method);
                HttpContext.Session.SetString("WalletBalance", newBalance.ToString("F2"));
                return Json(new { 
                    success = true, 
                    isPending = false,
                    amount,
                    newBalance, 
                    formatted = newBalance.ToString("N0") + " THB", 
                    formatted2 = newBalance.ToString("N2") + " THB", 
                    message = $"เติมเงิน +{amount:N2} THB สำเร็จ! ยอดเงินคงเหลือใหม่: {newBalance:N2} THB" 
                });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeductWalletAjax(decimal amount, string? title)
        {
            if (!IsLoggedIn()) return Json(new { success = false, message = "กรุณาเข้าสู่ระบบก่อน" });
            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            decimal currentBal = MockDataStore.GetUserBalance(userId);
            if (currentBal < amount)
            {
                return Json(new { success = false, message = $"ยอดเงินคงเหลือไม่พอ (มี {currentBal:N2} THB ขาดอีก {(amount - currentBal):N2} THB)" });
            }
            MockDataStore.DeductBalance(userId, amount, title ?? "ทดสอบหักเครดิตใช้งานระบบ", $"#TEST-{DateTime.Now:HHmmss}");
            decimal newBal = MockDataStore.GetUserBalance(userId);
            HttpContext.Session.SetString("WalletBalance", newBal.ToString("F2"));
            return Json(new { 
                success = true, 
                amount,
                newBalance = newBal, 
                formatted = newBal.ToString("N0") + " THB", 
                formatted2 = newBal.ToString("N2") + " THB", 
                message = $"หักเครดิต -{amount:N2} THB สำเร็จ! ยอดเงินคงเหลือใหม่: {newBal:N2} THB" 
            });
        }
    }
}
