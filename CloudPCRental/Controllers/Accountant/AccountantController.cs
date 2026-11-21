using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;
using System.Globalization;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์สำหรับฝ่ายบัญชีและการเงิน (Accountant)
    /// รับผิดชอบ: แดชบอร์ดรายได้รวม & กราฟ (Chart.js), ตรวจสอบสลิปโอนเงิน (Approve/Reject), จัดการคำขอคืนเงิน (Refunds)
    /// </summary>
    public class AccountantController : Controller
    {
        private readonly Csi402dbContext _context;
        private readonly IWebHostEnvironment? _env;

        public AccountantController(Csi402dbContext context, IWebHostEnvironment? env = null)
        {
            _context = context;
            _env = env;
        }

        // ==========================================
        // Helper Methods: ตรวจสอบสิทธิ์การเข้าถึง (เฉพาะฝ่ายบัญชีและการเงินเท่านั้น)
        // ==========================================
        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private bool IsAccountant()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Accountant" || role == "Admin";
        }

        private async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbBookings = await _context.Bookings
                        .Include(b => b.User)
                        .Include(b => b.Machine)
                        .OrderByDescending(b => b.PaymentDate)
                        .ToListAsync();

                    if (dbBookings.Any()) return dbBookings;
                }
            }
            catch { }

            return MockDataStore.GetBookings();
        }

        // ==========================================
        // 1. แดชบอร์ดรายได้รวมและกราฟ (Revenue Dashboard)
        // ==========================================
        [HttpGet]
        public IActionResult Index() => RedirectToAction("RevenueDashboard");

        [HttpGet]
        public IActionResult Dashboard() => RedirectToAction("RevenueDashboard");

        [HttpGet]
        public async Task<IActionResult> RevenueDashboard()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานส่วนงานบัญชี";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("RevenueDashboard", "Accountant") });
            }
            if (!IsAccountant())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงส่วนงานบัญชีและการเงิน (สงวนสิทธิ์เฉพาะพนักงานฝ่ายบัญชีเท่านั้น แอดมินไม่มีสิทธิ์จัดการข้อมูลการเงิน)";
                return RedirectToAction("AdminDashboard", "Admin");
            }

            var allBookings = await GetAllBookingsAsync();
            bool isDb = false;
            try { isDb = await _context.Database.CanConnectAsync(); } catch { }
            ViewBag.IsDbConnected = isDb;
            var approvedBookings = allBookings.Where(b => b.PaymentStatus == "Approved").ToList();

            var today = DateTime.Today;
            var currentMonth = today.Month;
            var currentYear = today.Year;

            var model = new RevenueDashboardViewModel
            {
                TotalRevenue = approvedBookings.Sum(b => b.TotalAmount),
                TodayRevenue = approvedBookings.Where(b => b.PaymentDate.Date == today).Sum(b => b.TotalAmount),
                MonthRevenue = approvedBookings.Where(b => b.PaymentDate.Month == currentMonth && b.PaymentDate.Year == currentYear).Sum(b => b.TotalAmount),
                PendingPaymentsCount = allBookings.Count(b => b.PaymentStatus == "Pending"),
                PendingRefundsCount = allBookings.Count(b => b.PaymentStatus == "RefundRequested"),
                TotalTransactionsCount = allBookings.Count,
                RecentTransactions = allBookings.OrderByDescending(b => b.PaymentDate).Take(100).ToList()
            };

            var thaiCulture = new CultureInfo("th-TH");

            // คำนวณเปรียบเทียบยอดขายระหว่างเดือน (Month-over-Month Comparison อิงตามข้อมูลจริง 100%)
            var firstDayThisMonth = new DateTime(currentYear, currentMonth, 1);
            var lastMonthDate = firstDayThisMonth.AddMonths(-1);
            int lastMonth = lastMonthDate.Month;
            int lastMonthYear = lastMonthDate.Year;

            decimal thisMonthRev = approvedBookings.Where(b => b.PaymentDate.Month == currentMonth && b.PaymentDate.Year == currentYear).Sum(b => b.TotalAmount);
            decimal lastMonthRev = approvedBookings.Where(b => b.PaymentDate.Month == lastMonth && b.PaymentDate.Year == lastMonthYear).Sum(b => b.TotalAmount);
            decimal diff = thisMonthRev - lastMonthRev;
            double growth = 0;
            string status = "เท่ากับเดือนที่แล้ว";
            string badgeClass = "text-secondary bg-secondary-subtle border-secondary-subtle";
            string icon = "bi-dash-lg";

            if (lastMonthRev > 0)
            {
                growth = (double)((diff / lastMonthRev) * 100);
                if (diff > 0)
                {
                    status = "ดีกว่าเดือนที่แล้ว";
                    badgeClass = "text-success bg-success-subtle border-success-subtle";
                    icon = "bi-graph-up-arrow";
                }
                else if (diff < 0)
                {
                    status = "น้อยกว่าเดือนที่แล้ว";
                    badgeClass = "text-danger bg-danger-subtle border-danger-subtle";
                    icon = "bi-graph-down-arrow";
                }
            }
            else
            {
                if (thisMonthRev > 0)
                {
                    growth = 100;
                    status = "ดีกว่าเดือนที่แล้ว (เริ่มมีรายได้เดือนแรก)";
                    badgeClass = "text-success bg-success-subtle border-success-subtle";
                    icon = "bi-graph-up-arrow";
                }
                else
                {
                    status = "ไม่มียอดขายทั้งสองเดือน";
                }
            }

            model.ThisMonthRevenue = thisMonthRev;
            model.LastMonthRevenue = lastMonthRev;
            model.MonthDifference = diff;
            model.GrowthRate = growth;
            model.ComparisonStatus = status;
            model.ComparisonBadgeClass = badgeClass;
            model.ComparisonIcon = icon;
            model.ThisMonthName = firstDayThisMonth.ToString("MMMM yyyy", thaiCulture);
            model.LastMonthName = lastMonthDate.ToString("MMMM yyyy", thaiCulture);

            // 1.1 กราฟรายได้รายวัน (Line Chart: ย้อนหลัง 7 วัน)
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = today.AddDays(-i);
                var dailySum = approvedBookings.Where(b => b.PaymentDate.Date == targetDate).Sum(b => b.TotalAmount);
                model.DailyLabels.Add(targetDate.ToString("dd MMM", thaiCulture));
                model.DailyRevenues.Add(dailySum);
            }

            // 1.2 กราฟรายได้รายสัปดาห์ (ย้อนหลัง 4 สัปดาห์)
            for (int w = 3; w >= 0; w--)
            {
                var startOfWeek = today.AddDays(-((w + 1) * 7) + 1);
                var endOfWeek = today.AddDays(-(w * 7));
                var weekSum = approvedBookings.Where(b => b.PaymentDate.Date >= startOfWeek && b.PaymentDate.Date <= endOfWeek).Sum(b => b.TotalAmount);
                model.WeeklyLabels.Add($"สัปดาห์ {4 - w}");
                model.WeeklyRevenues.Add(weekSum);
            }

            // 1.3 กราฟรายได้รายเดือน (Bar Chart: ม.ค. - ธ.ค. ในปีปัจจุบัน)
            string[] monthNames = { "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค." };
            for (int m = 1; m <= 12; m++)
            {
                var monthSum = approvedBookings.Where(b => b.PaymentDate.Year == currentYear && b.PaymentDate.Month == m).Sum(b => b.TotalAmount);
                model.MonthlyLabels.Add(monthNames[m - 1]);
                model.MonthlyRevenues.Add(monthSum);
            }

            return View(model);
        }

        // ==========================================
        // 2. ตรวจสอบสลิปโอนเงิน (Payment Verification)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PendingPayments()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAccountant())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ตรวจสอบสลิปโอนเงิน (สงวนสิทธิ์เฉพาะพนักงานฝ่ายบัญชีเท่านั้น)";
                return RedirectToAction("AdminDashboard", "Admin");
            }

            var allBookings = await GetAllBookingsAsync();
            bool isDb = false;
            try { isDb = await _context.Database.CanConnectAsync(); } catch { }
            ViewBag.IsDbConnected = isDb;
            var pendingPayments = allBookings
                .Where(b => b.PaymentStatus == "Pending")
                .OrderByDescending(b => b.PaymentDate)
                .ToList();

            return View(pendingPayments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePayment(int bookingId)
        {
            if (!IsLoggedIn() || !IsAccountant()) return RedirectToAction("Login", "Account");

            MockDataStore.ApproveBooking(bookingId);
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string accountant = HttpContext.Session.GetString("Username") ?? "accountant";
            MockDataStore.LogActivity(accountant, "Payment", $"อนุมัติรายการชำระเงิน #{bookingId}", "ข้อมูลทั่วไป", clientIp);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var booking = await _context.Bookings.Include(b => b.Machine).FirstOrDefaultAsync(b => b.BookingId == bookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Approved";
                        bool isTopUp = (booking.MachineId == 0) || (booking.Note != null && booking.Note.Contains("เติมเงิน"));
                        if (!isTopUp && booking.MachineId > 0 && booking.Machine != null && booking.Machine.Status == "Available")
                        {
                            booking.Machine.Status = "Rented";
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            var allBookings = await GetAllBookingsAsync();
            var approvedBooking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (approvedBooking != null && (approvedBooking.MachineId == 0 || (approvedBooking.Note != null && approvedBooking.Note.Contains("เติมเงิน"))))
            {
                TempData["SuccessMessage"] = $"อนุมัติสลิปเติมเงินรายการ #{bookingId} ยอด {approvedBooking.TotalAmount:N2} ฿ เรียบร้อยแล้ว (เครดิตได้ถูกโอนเข้ากระเป๋าเงินของลูกค้าทันที)";
            }
            else
            {
                TempData["SuccessMessage"] = $"อนุมัติสลิปการโอนเงินรายการ #{bookingId} เรียบร้อยแล้ว (เปิดสิทธิ์ให้ลูกค้าใช้งานทันที)";
            }
            return RedirectToAction("PendingPayments");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectPayment(int bookingId, string? note)
        {
            if (!IsLoggedIn() || !IsAccountant()) return RedirectToAction("Login", "Account");

            MockDataStore.RejectBooking(bookingId, note);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var booking = await _context.Bookings.FindAsync(bookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Rejected";
                        booking.Note = string.IsNullOrWhiteSpace(note) ? "สลิปไม่ถูกต้อง หรือยอดเงินไม่ตรงกับรายการ" : note;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"ปฏิเสธสลิปการโอนเงินของรายการ #{bookingId} เรียบร้อยแล้ว";
            return RedirectToAction("PendingPayments");
        }

        // ==========================================
        // 3. จัดการการยกเลิกและคืนเงิน (Refund Management)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Refunds()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAccountant())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าจัดการคำขอคืนเงิน (สงวนสิทธิ์เฉพาะพนักงานฝ่ายบัญชีเท่านั้น)";
                return RedirectToAction("AdminDashboard", "Admin");
            }

            var allBookings = await GetAllBookingsAsync();
            bool isDb = false;
            try { isDb = await _context.Database.CanConnectAsync(); } catch { }
            ViewBag.IsDbConnected = isDb;
            var refundList = allBookings
                .Where(b => b.PaymentStatus == "RefundRequested" || b.PaymentStatus == "Refunded" || b.PaymentStatus == "RefundRejected")
                .OrderByDescending(b => b.PaymentStatus == "RefundRequested")
                .ThenByDescending(b => b.RefundDate ?? b.PaymentDate)
                .ToList();

            return View(refundList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRefund(int bookingId, string? note)
        {
            if (!IsLoggedIn() || !IsAccountant()) return RedirectToAction("Login", "Account");

            // ดึงข้อมูลคำขอคืนเงินเพื่อระบุลูกค้าและยอดเงิน
            var allBookings = await GetAllBookingsAsync();
            var targetBooking = allBookings.FirstOrDefault(b => b.BookingId == bookingId) 
                             ?? MockDataStore.GetBookings().FirstOrDefault(b => b.BookingId == bookingId);

            if (targetBooking == null)
            {
                TempData["ErrorMessage"] = $"ไม่พบรายการคำขอคืนเงิน #{bookingId} ในระบบ";
                return RedirectToAction("Refunds");
            }

            int targetUserId = targetBooking.UserId;
            decimal refundAmount = targetBooking.TotalAmount;
            string machineName = targetBooking.Machine?.MachineName ?? "Cloud PC";

            string finalNote = string.IsNullOrWhiteSpace(note) 
                ? "ตรวจสอบเงื่อนไขตรงตามนโยบาย อนุมัติคืนเงินเต็มจำนวนเรียบร้อย (+60.00 ฿ เข้ากระเป๋าเงิน)" 
                : note;

            // 1. คืนเงินเข้ากระเป๋าเงินลูกค้าแบบเรียลไทม์
            decimal newBal = MockDataStore.RefundBalance(targetUserId, refundAmount, 
                $"ฝ่ายบัญชีอนุมัติคืนเงินคำขอ #{bookingId:D4} ({machineName})", $"#REFUND-{bookingId:D4}");

            // 2. อัปเดตสถานะใน MockDataStore
            MockDataStore.ApproveRefundBooking(bookingId, finalNote);

            // หากเป็นรายการที่เชื่อมโยงกับกลุ่ม seed data (103, 104, 94812) ให้ซิงค์พร้อมกันทั้งหมด
            if (bookingId == 103 || bookingId == 104 || bookingId == 94812)
            {
                MockDataStore.ApproveRefundBooking(103, finalNote);
                MockDataStore.ApproveRefundBooking(104, finalNote);
                MockDataStore.ApproveRefundBooking(94812, finalNote);
                MockDataStore.RefundBalance(5, 60.00m, "ฝ่ายบัญชีอนุมัติคืนเงินรายการ #CPC-94812", "#REFUND-94812");
                newBal = MockDataStore.GetUserBalance(5);
            }

            // ซิงค์ยอดเงินใน Session
            HttpContext.Session.SetString("WalletBalance", newBal.ToString("F2"));

            // 3. บันทึกใน MySQL Database หากเชื่อมต่ออยู่
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var booking = await _context.Bookings.Include(b => b.Machine).FirstOrDefaultAsync(b => b.BookingId == bookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Refunded";
                        booking.RefundDate = DateTime.Now;
                        booking.Note = finalNote;

                        if (booking.Machine != null && booking.Machine.Status == "Rented")
                        {
                            booking.Machine.Status = "Available";
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"ตรวจสอบเงื่อนไขและอนุมัติการคืนเงินสำหรับรายการ #{bookingId} สำเร็จเรียบร้อยแล้ว (โอนคืนเครดิต +{refundAmount:N2} THB เข้ากระเป๋าเงินลูกค้า ยอดใหม่: {newBal:N2} THB)";
            return RedirectToAction("Refunds");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRefund(int bookingId, string? note)
        {
            if (!IsLoggedIn() || !IsAccountant()) return RedirectToAction("Login", "Account");

            string finalNote = string.IsNullOrWhiteSpace(note) ? "ตรวจสอบแล้วไม่เข้าเงื่อนไขการคืนเงินของบริษัท" : note;
            MockDataStore.RejectRefundBooking(bookingId, finalNote);

            if (bookingId == 103 || bookingId == 104 || bookingId == 94812)
            {
                MockDataStore.RejectRefundBooking(103, finalNote);
                MockDataStore.RejectRefundBooking(104, finalNote);
                MockDataStore.RejectRefundBooking(94812, finalNote);
            }

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var booking = await _context.Bookings.FindAsync(bookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "RefundRejected";
                        booking.RefundDate = DateTime.Now;
                        booking.Note = finalNote;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"บันทึกผลการตรวจสอบ ปฏิเสธคำขอคืนเงินของรายการ #{bookingId} เรียบร้อยแล้ว";
            return RedirectToAction("Refunds");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessRefund(int bookingId, IFormFile? refundSlip, string? note)
        {
            if (!IsLoggedIn() || !IsAccountant()) return RedirectToAction("Login", "Account");

            // คืนเงินเข้ากระเป๋าเงินลูกค้าแบบเรียลไทม์
            var allBookings = await GetAllBookingsAsync();
            bool isDb = false;
            try { isDb = await _context.Database.CanConnectAsync(); } catch { }
            ViewBag.IsDbConnected = isDb;
            var targetBooking = allBookings.FirstOrDefault(b => b.BookingId == bookingId)
                             ?? MockDataStore.GetBookings().FirstOrDefault(b => b.BookingId == bookingId);

            if (targetBooking == null)
            {
                TempData["ErrorMessage"] = $"ไม่พบรายการคำขอคืนเงิน #{bookingId} ในระบบ";
                return RedirectToAction("Refunds");
            }

            MockDataStore.RefundBalance(targetBooking.UserId, targetBooking.TotalAmount, $"ฝ่ายบัญชีโอนคืนเงินคำขอ #{bookingId:D4} ({targetBooking.Machine?.MachineName ?? "Cloud PC"})", $"#REFUND-{bookingId:D4}");

            string? uploadedPath = null;

            if (refundSlip != null && refundSlip.Length > 0 && _env != null)
            {
                try
                {
                    var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "refund_slips");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var ext = Path.GetExtension(refundSlip.FileName);
                    var fileName = $"refund_{bookingId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await refundSlip.CopyToAsync(stream);
                    }

                    uploadedPath = $"/uploads/refund_slips/{fileName}";
                }
                catch { }
            }

            MockDataStore.ProcessRefundBooking(bookingId, uploadedPath, note);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var booking = await _context.Bookings.Include(b => b.Machine).FirstOrDefaultAsync(b => b.BookingId == bookingId);
                    if (booking != null)
                    {
                        booking.PaymentStatus = "Refunded";
                        booking.RefundDate = DateTime.Now;
                        if (!string.IsNullOrWhiteSpace(uploadedPath))
                        {
                            booking.RefundSlipImageUrl = uploadedPath;
                        }
                        if (!string.IsNullOrWhiteSpace(note))
                        {
                            booking.Note = note;
                        }

                        if (booking.Machine != null && booking.Machine.Status == "Rented")
                        {
                            booking.Machine.Status = "Available";
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"ดำเนินการคืนเงินและบันทึกข้อมูลรายการ #{bookingId} สำเร็จเรียบร้อยแล้ว";
            return RedirectToAction("Refunds");
        }
    }
}
