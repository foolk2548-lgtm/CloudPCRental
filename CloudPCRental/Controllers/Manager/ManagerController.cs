using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    public class ManagerController : Controller
    {
        private readonly Csi402dbContext _context;

        public ManagerController(Csi402dbContext context)
        {
            _context = context;
        }

        // ========== Auth Guard ==========
        private bool IsManager()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Manager" || role == "Admin";
        }

        private IActionResult RequireManager()
        {
            TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงส่วนนี้ กรุณาเข้าสู่ระบบในฐานะผู้จัดการ";
            return RedirectToAction("Login", "Account");
        }

        // ========== Helper: ดึงข้อมูล Bookings ==========
        private async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var list = await _context.Bookings
                        .Include(b => b.User)
                        .Include(b => b.Machine)
                        .OrderByDescending(b => b.BookingId)
                        .ToListAsync();
                    if (list.Any()) return list;
                }
            }
            catch { }
            return MockDataStore.GetBookings();
        }

        private async Task<List<Machine>> GetAllMachinesAsync()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var list = await _context.Machines.ToListAsync();
                    if (list.Any()) return list;
                }
            }
            catch { }
            return MockDataStore.GetMachines();
        }

        private async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var list = await _context.Users.ToListAsync();
                    if (list.Any()) return list;
                }
            }
            catch { }
            return MockDataStore.GetUsers();
        }

        // ========================================
        // Dashboard
        // ========================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (!IsManager()) return RequireManager();

            var bookings  = await GetAllBookingsAsync();
            var machines  = await GetAllMachinesAsync();
            var users     = await GetAllUsersAsync();
            var now       = DateTime.Now;

            // สถิติหลัก
            ViewBag.TotalMachines      = machines.Count;
            ViewBag.AvailableMachines  = machines.Count(m => m.Status == "Available" || m.Status == "ว่าง");
            ViewBag.RentedMachines     = machines.Count(m => m.Status == "Rented"    || m.Status == "ไม่ว่าง");
            ViewBag.MaintenanceMachines= machines.Count(m => m.Status == "Maintenance");
            ViewBag.TotalUsers         = users.Count(u => u.Role == "User");
            ViewBag.TotalBookings      = bookings.Count;

            // การจองวันนี้
            ViewBag.TodayBookings = bookings.Count(b => b.PaymentDate.Date == now.Date);

            // รายรับรวม (Approved เท่านั้น)
            var approvedBookings = bookings.Where(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)").ToList();
            ViewBag.TotalRevenue = approvedBookings.Sum(b => b.TotalAmount);

            // 5 การจองล่าสุด
            ViewBag.RecentBookings = bookings.Take(5).ToList();

            // สถานะเครื่องสรุป
            ViewBag.Machines = machines.OrderBy(m => m.MachineId).ToList();

            // การจองที่รอตรวจสอบ (Pending)
            ViewBag.PendingCount = bookings.Count(b => b.PaymentStatus == "Pending");

            // แจ้งซ่อมที่ค้าง
            ViewBag.MaintenanceCount = machines.Count(m => m.Status == "Maintenance");

            var managerName = HttpContext.Session.GetString("FullName") ?? HttpContext.Session.GetString("Username") ?? "ผู้จัดการ";
            ViewBag.ManagerName = managerName;

            return View();
        }

        // ========================================
        // ภาพรวมการจอง (Booking Overview)
        // ========================================
        [HttpGet]
        public async Task<IActionResult> BookingOverview()
        {
            if (!IsManager()) return RequireManager();

            var bookings = await GetAllBookingsAsync();
            var now = DateTime.Now;

            // กลุ่มสถานะ
            ViewBag.AllBookings        = bookings.OrderByDescending(b => b.BookingId).ToList();
            ViewBag.PendingBookings    = bookings.Where(b => b.PaymentStatus == "Pending").ToList();
            ViewBag.ApprovedBookings   = bookings.Where(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)").ToList();
            ViewBag.RefundRequests     = bookings.Where(b => b.PaymentStatus == "RefundRequested").ToList();
            ViewBag.TotalRevenue       = bookings.Where(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)").Sum(b => b.TotalAmount);
            ViewBag.TodayBookings      = bookings.Count(b => b.PaymentDate.Date == now.Date);

            return View();
        }

        // ========================================
        // สถานะเครื่อง (Machine Status)
        // ========================================
        [HttpGet]
        public async Task<IActionResult> MachineStatus()
        {
            if (!IsManager()) return RequireManager();

            var machines = await GetAllMachinesAsync();
            var bookings = await GetAllBookingsAsync();

            ViewBag.Machines     = machines.OrderBy(m => m.MachineId).ToList();
            ViewBag.ActiveRentals = bookings.Where(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)").ToList();
            ViewBag.Available    = machines.Count(m => m.Status == "Available" || m.Status == "ว่าง");
            ViewBag.Rented       = machines.Count(m => m.Status == "Rented"    || m.Status == "ไม่ว่าง");
            ViewBag.Maintenance  = machines.Count(m => m.Status == "Maintenance");

            return View();
        }

        // ========================================
        // รายงานรายรับ (Revenue Report)
        // ========================================
        [HttpGet]
        public async Task<IActionResult> RevenueReport()
        {
            if (!IsManager()) return RequireManager();

            var bookings = await GetAllBookingsAsync();
            var approved = bookings.Where(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)").ToList();

            ViewBag.TotalRevenue      = approved.Sum(b => b.TotalAmount);
            ViewBag.TotalTransactions = approved.Count;
            ViewBag.AvgPerBooking     = approved.Any() ? approved.Average(b => b.TotalAmount) : 0;

            // รายรับรายวัน 7 วันล่าสุด
            var last7 = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-i))
                .Select(d => new {
                    Date    = d.ToString("dd/MM"),
                    Revenue = approved.Where(b => b.PaymentDate.Date == d).Sum(b => b.TotalAmount)
                })
                .Reverse()
                .ToList();
            ViewBag.DailyRevenue = last7;

            // Top 5 เครื่องที่ทำรายได้สูงสุด
            var topMachines = approved
                .GroupBy(b => b.Machine?.MachineName ?? $"Machine #{b.MachineId}")
                .Select(g => new { Name = g.Key, Revenue = g.Sum(b => b.TotalAmount), Count = g.Count() })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToList();
            ViewBag.TopMachines = topMachines;

            ViewBag.RecentApproved = approved.OrderByDescending(b => b.BookingId).Take(10).ToList();

            return View();
        }
    }
}
