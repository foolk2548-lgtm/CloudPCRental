using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;
using System.Globalization;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์หลักสำหรับบริการคลาวด์คอมพิวเตอร์ (CloudPC)
    /// รับผิดชอบ: หน้าแรก (Index), แนะนำบริการ (HowToUse), สิทธิประโยชน์สมาชิก (Membership), ช่วยเหลือ (Help) และระบบข้อผิดพลาด (Error)
    /// (โมดูลการจอง, กระเป๋าเงิน, แชท, เครื่อง, และการตั้งค่าบัญชี ถูกแยกออกเป็น partial classes ตามฟังก์ชันงานอย่างเป็นระเบียบ)
    /// </summary>
    public partial class CloudPCController : Controller
    {
        private readonly Csi402dbContext _context;
        private readonly IWebHostEnvironment? _env;

        public CloudPCController(Csi402dbContext context, IWebHostEnvironment? env = null)
        {
            _context = context;
            _env = env;
        }

        // ==========================================
        // Helper Methods: จัดการข้อมูลร่วมกันระหว่าง Database และ Mock Store
        // ==========================================
        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private bool IsStaffOrAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin" || role == "Manager" || role == "Staff" || role == "Accountant";
        }
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Manager";
        private bool IsAdminOnly() => HttpContext.Session.GetString("Role") == "Admin";
        private bool IsAccountantOrAdmin()
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
                        .ToListAsync();

                    if (dbBookings.Any()) return dbBookings;
                }
            }
            catch { }

            return MockDataStore.GetBookings();
        }

        private async Task<List<Machine>> GetAllMachinesAsync()
        {
            List<Machine> list;
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbList = await _context.Machines.ToListAsync();
                    list = dbList.Any() ? dbList : MockDataStore.GetMachines();
                }
                else
                {
                    list = MockDataStore.GetMachines();
                }
            }
            catch
            {
                list = MockDataStore.GetMachines();
            }

            // ซิงค์ข้อมูลสถานะการแจ้งซ่อมและหมายเหตุจาก MockDataStore เสมอ
            // ป้องกันปัญหาเครื่องที่แจ้งซ่อม (รออนุมัติ หรือกำลังซ่อม) ขึ้นว่า "กำลังใช้งาน"
            var mockMachines = MockDataStore.GetMachines();
            foreach (var m in list)
            {
                var mock = mockMachines.FirstOrDefault(x => x.MachineId == m.MachineId || x.MachineName == m.MachineName);
                if (mock != null)
                {
                    if (!string.IsNullOrEmpty(mock.RepairNote)) m.RepairNote = mock.RepairNote;
                    if (!string.IsNullOrEmpty(mock.ReportedBy)) m.ReportedBy = mock.ReportedBy;
                    if (mock.ReportedAt.HasValue) m.ReportedAt = mock.ReportedAt;
                    if (!string.IsNullOrEmpty(mock.ApprovedBy)) m.ApprovedBy = mock.ApprovedBy;

                    if (mock.Status == "PendingRepair" || mock.Status == "Maintenance")
                    {
                        m.Status = mock.Status;
                    }
                    else if (mock.Status == "Available" && (m.Status == "PendingRepair" || m.Status == "Maintenance"))
                    {
                        m.Status = "Available";
                    }
                }
            }

            return list;
        }

        private async Task<Machine?> GetMachineByIdAsync(int id)
        {
            Machine? machine = null;
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    machine = await _context.Machines.FindAsync(id);
                }
            }
            catch { }

            if (machine == null)
            {
                machine = MockDataStore.GetMachineById(id);
            }
            else
            {
                var mock = MockDataStore.GetMachineById(id) ?? MockDataStore.GetMachines().FirstOrDefault(x => x.MachineName == machine.MachineName);
                if (mock != null)
                {
                    if (!string.IsNullOrEmpty(mock.RepairNote)) machine.RepairNote = mock.RepairNote;
                    if (!string.IsNullOrEmpty(mock.ReportedBy)) machine.ReportedBy = mock.ReportedBy;
                    if (mock.ReportedAt.HasValue) machine.ReportedAt = mock.ReportedAt;
                    if (!string.IsNullOrEmpty(mock.ApprovedBy)) machine.ApprovedBy = mock.ApprovedBy;

                    if (mock.Status == "PendingRepair" || mock.Status == "Maintenance")
                    {
                        machine.Status = mock.Status;
                    }
                    else if (mock.Status == "Available" && (machine.Status == "PendingRepair" || machine.Status == "Maintenance"))
                    {
                        machine.Status = "Available";
                    }
                }
            }

            return machine;
        }

        private async Task<List<User>> GetAllUsersAsync()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbUsers = await _context.Users.ToListAsync();
                    if (dbUsers.Any()) return dbUsers;
                }
            }
            catch { }
            return MockDataStore.GetUsers();
        }

        private async Task<User?> AuthenticateUserAsync(string usernameOrEmail, string password)
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var u = await _context.Users.FirstOrDefaultAsync(x => 
                        (x.Username == usernameOrEmail || x.Email == usernameOrEmail) && x.Password == password);
                    if (u != null) return u;
                }
            }
            catch { }
            return MockDataStore.Authenticate(usernameOrEmail, password);
        }

        // ==========================================
        // 1. หน้าแรก (Home / Landing) - Wireframe Page 2
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var role = HttpContext.Session.GetString("Role");
            if (role == "Admin" || role == "Staff") return RedirectToAction("AdminDashboard", "Admin");
            if (role == "Manager") return RedirectToAction("Dashboard", "Manager");
            if (role == "Accountant") return RedirectToAction("RevenueDashboard", "Accountant");

            var machines = await GetAllMachinesAsync();
            var popularMachines = machines.Take(4).ToList();
            return View(popularMachines);
        }

        // ==========================================
        // 8. แดชบอร์ดลูกค้า: ระบบสมาชิกและคะแนนสะสม - Wireframe Page 8
        // ==========================================
        [HttpGet]
        public IActionResult Membership()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อดูสิทธิประโยชน์สมาชิกและคะแนนสะสม";
                return RedirectToAction("Login", new { returnUrl = Url.Action("Membership", "CloudPC") });
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            string username = HttpContext.Session.GetString("Username") ?? "thanapol";
            var model = MockDataStore.GetUserMembershipInfo(userId, username);
            return View(model);
        }

        // ==========================================
        // วิธีใช้งานระบบ (How to Use)
        // ==========================================
        [HttpGet]
        public IActionResult HowToUse()
        {
            return View();
        }

        // ==========================================
        // ศูนย์ช่วยเหลือและคำถามที่พบบ่อย (Help & FAQ)
        // ==========================================
        [HttpGet]
        public IActionResult Help()
        {
            return View();
        }

        // --- หน้าแสดงข้อผิดพลาดของระบบ (Error Handling) ---
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml", new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
