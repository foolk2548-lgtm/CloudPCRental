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

            // สถิติการดำเนินงานหลัก (Operations Metrics)
            int totalM = machines.Count;
            int availableM = machines.Count(m => m.Status == "Available" || m.Status == "ว่าง");
            int rentedM = machines.Count(m => m.Status == "Rented" || m.Status == "ไม่ว่าง");
            int maintM = machines.Count(m => m.Status == "Maintenance" || m.Status == "PendingRepair");

            ViewBag.TotalMachines       = totalM;
            ViewBag.AvailableMachines   = availableM;
            ViewBag.RentedMachines      = rentedM;
            ViewBag.MaintenanceMachines = maintM;
            ViewBag.UtilizationRate     = totalM > 0 ? (int)Math.Round((double)rentedM * 100.0 / totalM) : 0;

            ViewBag.TotalUsers          = users.Count(u => u.Role == "User");
            ViewBag.TotalBookings       = bookings.Count;
            ViewBag.TodayBookings       = bookings.Count(b => b.PaymentDate.Date == now.Date);
            ViewBag.TotalRentalHours    = bookings.Where(b => b.PaymentStatus != "Refunded" && b.PaymentStatus != "Rejected").Sum(b => b.RentalHours);

            // 5 การจองล่าสุด
            ViewBag.RecentBookings      = bookings.Take(5).ToList();

            // รายการเครื่อง
            ViewBag.Machines            = machines.OrderBy(m => m.MachineId).ToList();

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

            // กลุ่มสถานะการจอง
            ViewBag.AllBookings        = bookings.OrderByDescending(b => b.BookingId).ToList();
            ViewBag.ApprovedBookings   = bookings.Where(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)" || b.PaymentStatus == "InUse").ToList();
            ViewBag.TodayBookings      = bookings.Count(b => b.PaymentDate.Date == now.Date);
            ViewBag.TotalRentalHours   = bookings.Where(b => b.PaymentStatus != "Refunded" && b.PaymentStatus != "Rejected").Sum(b => b.RentalHours);

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

            int totalM = machines.Count;
            int availableM = machines.Count(m => m.Status == "Available" || m.Status == "ว่าง");
            int rentedM = machines.Count(m => m.Status == "Rented" || m.Status == "ไม่ว่าง");
            int maintM = machines.Count(m => m.Status == "Maintenance" || m.Status == "PendingRepair");

            ViewBag.Machines        = machines.OrderBy(m => m.MachineId).ToList();
            ViewBag.ActiveRentals   = bookings.Where(b => b.PaymentStatus == "InUse" || b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)").ToList();
            ViewBag.Total           = totalM;
            ViewBag.Available       = availableM;
            ViewBag.Rented          = rentedM;
            ViewBag.Maintenance     = maintM;
            ViewBag.UtilizationRate = totalM > 0 ? (int)Math.Round((double)rentedM * 100.0 / totalM) : 0;

            return View();
        }

        // ========================================
        // รายงานรายรับ: การเงินถูกย้ายไปให้ฝ่ายบัญชี (Accountant) ดูแลโดยตรง
        // ========================================
        [HttpGet]
        public IActionResult RevenueReport()
        {
            // หน้าที่ด้านการเงินและรายงานรายรับอยู่ในความรับผิดชอบของฝ่ายบัญชี
            return RedirectToAction("Dashboard");
        }
    }
}
