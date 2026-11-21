using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// ส่วนการจัดการข้อมูลเครื่องคอมพิวเตอร์และการแจ้งซ่อมบำรุง (Machine CRUD & Repair Workflow)
    /// </summary>
    public partial class AdminController : Controller
    {
        // ==========================================
        // จัดการข้อมูลคอมพิวเตอร์ (Machine CRUD & Repair Workflow)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageMachines()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งาน";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ManageMachines", "Admin") });
            }
            if (HttpContext.Session.GetString("Role") == "Accountant")
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าจัดการเครื่องคอมพิวเตอร์ (สำหรับพนักงานและผู้จัดการ)";
                return RedirectToAction("RevenueDashboard", "Accountant");
            }
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้านี้";
                return RedirectToAction("Index", "CloudPC");
            }

            var machines = await GetAllMachinesAsync();
            return View(machines);
        }

        [HttpGet]
        public IActionResult CreateMachine()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "พนักงานไม่มีสิทธิ์เพิ่มเครื่องใหม่ (สำหรับผู้จัดการหรือแอดมินเท่านั้น)";
                return RedirectToAction("ManageMachines");
            }
            return View(new Machine());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateMachine(Machine machine)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "พนักงานไม่มีสิทธิ์เพิ่มเครื่องใหม่ (สำหรับผู้จัดการหรือแอดมินเท่านั้น)";
                return RedirectToAction("ManageMachines");
            }
            if (!ModelState.IsValid) return View(machine);

            MockDataStore.AddMachine(machine);
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string adminUser = HttpContext.Session.GetString("Username") ?? "admin";
            MockDataStore.LogActivity(adminUser, "Hardware", $"เพิ่มเครื่องคอมพิวเตอร์ใหม่ {machine.MachineName} เข้าสู่ระบบ", "ข้อมูลทั่วไป", clientIp);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    _context.Machines.Add(machine);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"เพิ่มเครื่อง {machine.MachineName} เรียบร้อยแล้ว";
            return RedirectToAction("ManageMachines");
        }

        [HttpGet]
        public async Task<IActionResult> EditMachine(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "พนักงานไม่สามารถแก้ไขสเปกหรือเปลี่ยนสถานะเครื่องโดยตรงได้ หากเครื่องมีปัญหาโปรดกดปุ่ม 'แจ้งซ่อม' เพื่อส่งหมายเหตุถึงผู้จัดการ";
                return RedirectToAction("ManageMachines");
            }

            var machine = await GetMachineByIdAsync(id);
            if (machine == null) return RedirectToAction("ManageMachines");
            return View(machine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditMachine(Machine machine)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "พนักงานไม่สามารถแก้ไขสเปกหรือเปลี่ยนสถานะเครื่องโดยตรงได้ หากเครื่องมีปัญหาโปรดกดปุ่ม 'แจ้งซ่อม' เพื่อส่งหมายเหตุถึงผู้จัดการ";
                return RedirectToAction("ManageMachines");
            }
            if (!ModelState.IsValid) return View(machine);

            MockDataStore.UpdateMachine(machine);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    _context.Machines.Update(machine);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"แก้ไขข้อมูลเครื่อง {machine.MachineName} สำเร็จ";
            return RedirectToAction("ManageMachines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMachine(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "พนักงานไม่มีสิทธิ์ลบเครื่องคอมพิวเตอร์ (สำหรับผู้จัดการหรือแอดมินเท่านั้น)";
                return RedirectToAction("ManageMachines");
            }

            MockDataStore.DeleteMachine(id);
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string adminUser = HttpContext.Session.GetString("Username") ?? "admin";
            MockDataStore.LogActivity(adminUser, "Hardware", $"ลบเครื่องคอมพิวเตอร์รหัส PC-{id:D3} ออกจากระบบ", "วิกฤต", clientIp);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var machine = await _context.Machines.FindAsync(id);
                    if (machine != null)
                    {
                        _context.Machines.Remove(machine);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = "ลบเครื่องคอมพิวเตอร์เรียบร้อยแล้ว";
            return RedirectToAction("ManageMachines");
        }

        // ==========================================
        // ระบบแจ้งซ่อมและอนุมัติซ่อม (Repair Workflow Actions)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRepair(int machineId, string issueType, string note)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ดำเนินการแจ้งซ่อม";
                return RedirectToAction("Index", "CloudPC");
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                TempData["ErrorMessage"] = "กรุณาระบุรายละเอียดหรือหมายเหตุอาการเสียส่งถึงผู้จัดการ";
                return RedirectToAction("ManageMachines");
            }

            var reporter = HttpContext.Session.GetString("Username") ?? "พนักงานหน้าร้าน";
            var role = HttpContext.Session.GetString("Role") ?? "Staff";
            var reporterWithRole = $"{reporter} ({role})";

            MockDataStore.RequestMachineRepair(machineId, issueType, note, reporterWithRole);
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            MockDataStore.LogActivity(reporterWithRole, "Hardware", $"แจ้งซ่อมเครื่อง PC-{machineId:D3} ({issueType}): {note}", "สำคัญ", clientIp);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var m = await _context.Machines.FindAsync(machineId);
                    if (m != null)
                    {
                        m.Status = "PendingRepair";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"ส่งเรื่องแจ้งซ่อมเครื่อง PC-{machineId:D3} ไปยังผู้จัดการเรียบร้อยแล้ว (สถานะ: รอผู้จัดการอนุมัติซ่อม)";
            return RedirectToAction("ManageMachines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRepair(int machineId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "เฉพาะผู้จัดการหรือแอดมินเท่านั้นที่มีสิทธิ์อนุมัติการซ่อมบำรุง";
                return RedirectToAction("ManageMachines");
            }

            var approver = HttpContext.Session.GetString("Username") ?? "ผู้จัดการ";
            var role = HttpContext.Session.GetString("Role") ?? "Manager";
            var approverWithRole = $"{approver} ({role})";

            MockDataStore.ApproveMachineRepair(machineId, approverWithRole);
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            MockDataStore.LogActivity(approverWithRole, "Hardware", $"อนุมัติการแจ้งซ่อมเครื่อง PC-{machineId:D3} และส่งมอบช่างเทคนิค", "สำคัญ", clientIp);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var m = await _context.Machines.FindAsync(machineId);
                    if (m != null)
                    {
                        m.Status = "Maintenance";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"อนุมัติการแจ้งซ่อมเครื่อง PC-{machineId:D3} เรียบร้อยแล้ว (สถานะเปลี่ยนเป็นกำลังซ่อมบำรุง และระบบได้ส่งเรื่องแจ้งช่างเทคนิคเรียบร้อยแล้ว)";
            return RedirectToAction("ManageMachines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRepair(int machineId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "เฉพาะผู้จัดการหรือแอดมินเท่านั้นที่มีสิทธิ์ปฏิเสธคำขอซ่อม";
                return RedirectToAction("ManageMachines");
            }

            MockDataStore.RejectMachineRepair(machineId);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var m = await _context.Machines.FindAsync(machineId);
                    if (m != null)
                    {
                        m.Status = "Available";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"ปฏิเสธคำขอแจ้งซ่อมเครื่อง PC-{machineId:D3} แล้ว ปรับสถานะเครื่องเป็นพร้อมใช้งานตามปกติ";
            return RedirectToAction("ManageMachines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinishRepair(int machineId)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "เฉพาะผู้จัดการหรือแอดมินเท่านั้นที่มีสิทธิ์บันทึกการซ่อมเสร็จสิ้น";
                return RedirectToAction("ManageMachines");
            }

            MockDataStore.FinishMachineRepair(machineId);
            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string approver = HttpContext.Session.GetString("Username") ?? "ผู้จัดการ";
            MockDataStore.LogActivity(approver, "Hardware", $"บันทึกช่างซ่อมเครื่อง PC-{machineId:D3} เสร็จสมบูรณ์แล้ว", "ข้อมูลทั่วไป", clientIp);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var m = await _context.Machines.FindAsync(machineId);
                    if (m != null)
                    {
                        m.Status = "Available";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"บันทึกช่างซ่อมเครื่อง PC-{machineId:D3} เสร็จสมบูรณ์แล้ว เครื่องกลับมาพร้อมเปิดให้บริการทันที";
            return RedirectToAction("ManageMachines");
        }

        // ==========================================
        // จัดการเครื่องที่กำลังใช้งานในกรณีมีปัญหา (Emergency Machine Operations)
        // ==========================================
        [HttpPost]
        public async Task<IActionResult> RestartMachine(int machineId)
        {
            if (!IsLoggedIn() || !IsStaffOrAdmin())
            {
                return Json(new { success = false, message = "คุณไม่มีสิทธิ์สั่งการเครื่องคอมพิวเตอร์" });
            }

            var machine = await GetMachineByIdAsync(machineId);
            string mCode = $"PC-{machineId:D3}";
            string mName = machine?.MachineName ?? mCode;

            return Json(new
            {
                success = true,
                message = $"ส่งสัญญาณรีสตาร์ทเครื่อง {mName} ({mCode}) ทางไกลสำเร็จเรียบร้อยแล้ว! ระบบกำลังรีบูตและเชื่อมต่อใหม่ภายใน 30 วินาที"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmergencyReportRepair(int machineId, string issueType, string note, bool refundCustomer = true)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ดำเนินการนี้";
                return RedirectToAction("Index", "CloudPC");
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                TempData["ErrorMessage"] = "กรุณาระบุรายละเอียดหรือหมายเหตุอาการเสียของเครื่อง";
                return RedirectToAction("ManageMachines");
            }

            var reporter = $"{HttpContext.Session.GetString("Username") ?? "เจ้าหน้าที่"} ({HttpContext.Session.GetString("Role") ?? "Staff"})";
            var result = MockDataStore.EmergencyReportRepair(machineId, issueType, note, refundCustomer, reporter);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var m = await _context.Machines.FindAsync(machineId);
                    if (m != null)
                    {
                        m.Status = "PendingRepair";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = result.message;
            return RedirectToAction("ManageMachines");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EmergencyReleaseMachine(int machineId, string reason, string nextStatus = "Available", bool refundCustomer = true)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsStaffOrAdmin())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์ดำเนินการนี้";
                return RedirectToAction("Index", "CloudPC");
            }

            var opName = $"{HttpContext.Session.GetString("Username") ?? "เจ้าหน้าที่"} ({HttpContext.Session.GetString("Role") ?? "Staff"})";
            var result = MockDataStore.EmergencyReleaseMachine(machineId, reason, nextStatus, refundCustomer, opName);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var m = await _context.Machines.FindAsync(machineId);
                    if (m != null)
                    {
                        m.Status = nextStatus == "PendingRepair" ? "PendingRepair" : "Available";
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = result.message;
            return RedirectToAction("ManageMachines");
        }
    }
}
