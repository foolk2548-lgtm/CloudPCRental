using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์หลักสำหรับผู้ดูแลระบบ (Admin, Manager, Staff)
    /// รับผิดชอบ: แดชบอร์ดภาพรวมระบบ, บันทึกประวัติระบบ (System Logs), และตั้งค่าทั่วไป (General Settings)
    /// (ส่วนงานเครื่อง, ผู้ใช้, และแชท ได้ถูกแยกออกเป็น AdminController.Machines.cs, AdminController.Users.cs, AdminController.Support.cs เพื่อความกระชับและเป็นระเบียบ)
    /// </summary>
    public partial class AdminController : Controller
    {
        private readonly Csi402dbContext _context;

        public AdminController(Csi402dbContext context)
        {
            _context = context;
        }

        // ==========================================
        // Helper Methods: ตรวจสอบสิทธิ์และดึงข้อมูลส่วนกลาง
        // ==========================================
        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private bool IsStaffOrAdmin()
        {
            var role = HttpContext.Session.GetString("Role");
            return role == "Admin" || role == "Manager" || role == "Staff" || role == "Accountant";
        }
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Manager";
        private bool IsAdminOnly() => HttpContext.Session.GetString("Role") == "Admin";

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

            // ซิงค์ข้อมูลการแจ้งซ่อม (หมายเหตุ, ผู้แจ้ง, วันที่แจ้ง, ผู้อนุมัติ) จาก MockDataStore
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
                    
                    if ((mock.Status == "PendingRepair" || mock.Status == "Maintenance") && m.Status != mock.Status)
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
                var mock = MockDataStore.GetMachineById(id);
                if (mock != null)
                {
                    if (!string.IsNullOrEmpty(mock.RepairNote)) machine.RepairNote = mock.RepairNote;
                    if (!string.IsNullOrEmpty(mock.ReportedBy)) machine.ReportedBy = mock.ReportedBy;
                    if (mock.ReportedAt.HasValue) machine.ReportedAt = mock.ReportedAt;
                    if (!string.IsNullOrEmpty(mock.ApprovedBy)) machine.ApprovedBy = mock.ApprovedBy;
                    if ((mock.Status == "PendingRepair" || mock.Status == "Maintenance") && machine.Status != mock.Status)
                    {
                        machine.Status = mock.Status;
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

        // ==========================================
        // 1. แดชบอร์ดหลักผู้ดูแลระบบ (Admin Dashboard)
        // ==========================================
        [HttpGet]
        public IActionResult Index() => RedirectToAction("AdminDashboard");

        [HttpGet]
        public IActionResult Dashboard() => RedirectToAction("AdminDashboard");

        [HttpGet]
        public async Task<IActionResult> AdminDashboard()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งานแดชบอร์ดจัดการระบบ";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("AdminDashboard", "Admin") });
            }

            if (HttpContext.Session.GetString("Role") == "Accountant")
            {
                return RedirectToAction("RevenueDashboard", "Accountant");
            }

            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้านี้";
                return RedirectToAction("Index", "CloudPC");
            }

            bool isDb = false;
            try { isDb = await _context.Database.CanConnectAsync(); } catch { }
            ViewBag.IsDbConnected = isDb;

            var machines = await GetAllMachinesAsync();
            var users = await GetAllUsersAsync();
            ViewBag.TotalUsers = users.Count;
            return View(machines);
        }

        // ==========================================
        // 2. บันทึกระบบและการตั้งค่าทั่วไป (System Logs & Settings)
        // ==========================================
        [HttpGet]
        public IActionResult SystemLogs()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdminOnly())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงบันทึกระบบ";
                return RedirectToAction("AdminDashboard");
            }
            return View();
        }

        [HttpGet]
        public IActionResult GeneralSettings()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdminOnly())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงการตั้งค่าทั่วไป";
                return RedirectToAction("AdminDashboard");
            }
            return View();
        }
    }
}
