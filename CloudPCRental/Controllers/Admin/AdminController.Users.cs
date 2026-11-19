using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// ส่วนการจัดการผู้ใช้งานและสิทธิ์ (User CRUD & Role Permissions - เฉพาะ Admin เท่านั้น)
    /// </summary>
    public partial class AdminController : Controller
    {
        // ==========================================
        // จัดการผู้ใช้งานและสิทธิ์ (User CRUD - เฉพาะ Admin เท่านั้น)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> ManageUsers()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อนเข้าใช้งาน";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("ManageUsers", "Admin") });
            }
            if (!IsAdminOnly())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงหน้าจัดการผู้ใช้งาน (เฉพาะผู้ดูแลระบบ Admin เท่านั้น)";
                return RedirectToAction("AdminDashboard");
            }

            var users = await GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        public IActionResult CreateUser()
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdminOnly())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เพิ่มผู้ใช้งาน (เฉพาะ Admin เท่านั้น)";
                return RedirectToAction("AdminDashboard");
            }
            return View(new User());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(User user)
        {
            if (!IsLoggedIn() || !IsAdminOnly()) return RedirectToAction("AdminDashboard");
            if (user.Role == "Admin")
            {
                ModelState.AddModelError("Role", "ระบบอนุญาตให้มีผู้ดูแลระบบหลัก (Admin) ได้เพียง 1 บัญชีเท่านั้น และไม่อนุญาตให้สร้างแอดมินเพิ่ม");
            }
            if (!ModelState.IsValid) return View(user);

            MockDataStore.AddUser(user);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            TempData["SuccessMessage"] = $"เพิ่มผู้ใช้งาน {user.Username} ({user.Role}) สำเร็จ";
            return RedirectToAction("ManageUsers");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdminOnly())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์แก้ไขข้อมูลผู้ใช้งาน (เฉพาะ Admin เท่านั้น)";
                return RedirectToAction("AdminDashboard");
            }

            var user = (await GetAllUsersAsync()).FirstOrDefault(u => u.UserId == id);
            if (user == null) return RedirectToAction("ManageUsers");
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(User user)
        {
            if (!IsLoggedIn() || !IsAdminOnly()) return RedirectToAction("AdminDashboard");
            if (!ModelState.IsValid) return View(user);

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var currentUsername = HttpContext.Session.GetString("Username") ?? "";

            bool isPrimaryAdmin = user.UserId == 1 || user.Username.Equals("admin", StringComparison.OrdinalIgnoreCase);
            bool isSelf = (currentUserId.HasValue && user.UserId == currentUserId.Value) || isPrimaryAdmin;

            // ไม่อนุญาตให้แต่งตั้งบัญชีอื่นเป็น Admin
            if (!isPrimaryAdmin && user.Role == "Admin")
            {
                ModelState.AddModelError("Role", "ระบบอนุญาตให้มีผู้ดูแลระบบหลัก (Admin) ได้เพียง 1 บัญชีเท่านั้น ไม่สามารถแต่งตั้งผู้ใช้อื่นเป็น Admin ได้");
            }
            if (!ModelState.IsValid) return View(user);

            if (isPrimaryAdmin)
            {
                user.Role = "Admin"; // ล็อกสิทธิ์ของแอดมินหลักไว้เสมอ
            }

            MockDataStore.UpdateUser(user);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            TempData["SuccessMessage"] = isSelf
                ? $"บันทึกการแก้ไขข้อมูลบัญชีของคุณ ({user.Username}) เรียบร้อยแล้ว"
                : $"แก้ไขข้อมูลและปรับสิทธิ์ของ {user.Username} เป็น {user.Role} เรียบร้อยแล้ว";

            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            if (!IsLoggedIn() || !IsAdminOnly()) return RedirectToAction("AdminDashboard");

            var currentUserId = HttpContext.Session.GetInt32("UserId");
            var currentUsername = HttpContext.Session.GetString("Username") ?? "";

            bool isSelf = currentUserId.HasValue && id == currentUserId.Value;
            bool isPrimaryAdmin = id == 1;

            if (isSelf || isPrimaryAdmin)
            {
                TempData["ErrorMessage"] = "ไม่สามารถลบบัญชีผู้ดูแลระบบหลัก (Admin) หรือบัญชีที่กำลังล็อกอินอยู่ได้ เพื่อความปลอดภัยของระบบ";
                return RedirectToAction("ManageUsers");
            }

            MockDataStore.DeleteUser(id);
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var user = await _context.Users.FindAsync(id);
                    if (user != null)
                    {
                        _context.Users.Remove(user);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = "ลบผู้ใช้งานเรียบร้อยแล้ว";
            return RedirectToAction("ManageUsers");
        }
    }
}
