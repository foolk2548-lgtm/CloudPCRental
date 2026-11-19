using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// โมดูลตั้งค่าบัญชีส่วนตัว และการเชื่อมโยงระบบเดิม (Account Settings & Route Forwarders)
    /// </summary>
    public partial class CloudPCController : Controller
    {
        // ==========================================
        // 8.2 แดชบอร์ดลูกค้า: ตั้งค่าบัญชี (Account Settings)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AccountSettings()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อตั้งค่าบัญชีของคุณ";
                return RedirectToAction("Login", new { returnUrl = Url.Action("AccountSettings", "CloudPC") });
            }

            var username = HttpContext.Session.GetString("Username") ?? "ธนพล ดีสมเกียรติ";
            var email = HttpContext.Session.GetString("Email") ?? "thanapol@example.com";
            var userId = HttpContext.Session.GetInt32("UserId") ?? 3;

            var model = new AccountSettingsViewModel
            {
                UserId = userId,
                FullName = username,
                Username = username.Contains(" ") ? "thanapol" : username,
                Email = email,
                PhoneNumber = MockDataStore.GetUserPhoneNumber(userId)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(AccountSettingsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.FullName) || string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ErrorMessage"] = "กรุณากรอกข้อมูลชื่อและอีเมลให้ครบถ้วน";
                return View("AccountSettings", model);
            }

            if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
            {
                var cleanPhone = System.Text.RegularExpressions.Regex.Replace(model.PhoneNumber, @"\D", "");
                if (cleanPhone.Length != 10 || !cleanPhone.StartsWith("0"))
                {
                    TempData["ErrorMessage"] = "เบอร์โทรศัพท์ไม่ถูกต้อง กรุณาระบุตัวเลข 10 หลัก (เช่น 081-234-5678)";
                    return View("AccountSettings", model);
                }
                MockDataStore.SetUserPhoneNumber(model.UserId, model.PhoneNumber);
            }

            HttpContext.Session.SetString("Username", model.FullName);
            HttpContext.Session.SetString("Email", model.Email);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user != null)
                    {
                        user.Email = model.Email;
                        user.Username = model.Username;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = "บันทึกและปรับปรุงข้อมูลส่วนตัวเรียบร้อยแล้ว";
            return RedirectToAction("AccountSettings");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(AccountSettingsViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                TempData["ErrorMessage"] = "กรุณากรอกรหัสผ่านปัจจุบันและรหัสผ่านใหม่";
                return View("AccountSettings", model);
            }

            if (model.NewPassword != model.ConfirmNewPassword)
            {
                TempData["ErrorMessage"] = "รหัสผ่านใหม่และการยืนยันรหัสผ่านไม่ตรงกัน";
                return View("AccountSettings", model);
            }

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var user = await _context.Users.FindAsync(model.UserId);
                    if (user != null)
                    {
                        user.Password = model.NewPassword;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = "เปลี่ยนรหัสผ่านสำเร็จเรียบร้อยแล้ว";
            return RedirectToAction("AccountSettings");
        }

        // ==========================================
        // Backward Compatibility Forwarders / Aliases
        // เชื่อมโยงไปยังคอนโทรลเลอร์ที่แยกออกไป เพื่อให้ URL เดิมทำงานได้สมบูรณ์
        // (AccountController, AdminController, AccountantController)
        // ==========================================

        // --- เชื่อมโยงไปยัง AccountController (ระบบสมาชิก & Authentication) ---
        [HttpGet]
        public IActionResult Login(string? returnUrl = null) => RedirectToAction("Login", "Account", new { returnUrl });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            var user = await AuthenticateUserAsync(model.UsernameOrEmail, model.Password);
            if (user == null)
            {
                TempData["ErrorMessage"] = "ชื่อผู้ใช้ / อีเมล หรือรหัสผ่านไม่ถูกต้อง (หากบัญชีถูกลบออกจากระบบ กรุณาสมัครสมาชิกใหม่)";
                return RedirectToAction("Login", "Account", new { returnUrl });
            }
            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role);
            decimal bal = MockDataStore.GetUserBalance(user.UserId);
            HttpContext.Session.SetString("WalletBalance", bal.ToString("F2"));
            TempData["SuccessMessage"] = $"เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับคุณ {user.Username}";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            if (user.Role == "Accountant") return RedirectToAction("RevenueDashboard", "Accountant");
            if (user.Role == "Admin" || user.Role == "Manager" || user.Role == "Staff") return RedirectToAction("AdminDashboard", "Admin");
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Register() => RedirectToAction("Register", "Account");

        [HttpGet]
        public IActionResult ForgotPassword() => RedirectToAction("ForgotPassword", "Account");

        [HttpGet]
        public IActionResult Logout() => RedirectToAction("Logout", "Account");

        [HttpGet]
        public IActionResult QuickLogin(string? returnUrl = null, string accountType = "user") =>
            RedirectToAction("QuickLogin", "Account", new { returnUrl, accountType });

        // --- เชื่อมโยงไปยัง AdminController (ระบบผู้ดูแลระบบ) ---
        [HttpGet]
        public IActionResult AdminDashboard() => RedirectToAction("AdminDashboard", "Admin");

        [HttpGet]
        public IActionResult ManageMachines() => RedirectToAction("ManageMachines", "Admin");

        [HttpGet]
        public IActionResult CreateMachine() => RedirectToAction("CreateMachine", "Admin");

        [HttpGet]
        public IActionResult EditMachine(int id) => RedirectToAction("EditMachine", "Admin", new { id });

        [HttpGet]
        public IActionResult ManageUsers() => RedirectToAction("ManageUsers", "Admin");

        [HttpGet]
        public IActionResult CreateUser() => RedirectToAction("CreateUser", "Admin");

        [HttpGet]
        public IActionResult EditUser(int id) => RedirectToAction("EditUser", "Admin", new { id });

        [HttpGet]
        public IActionResult SystemLogs() => RedirectToAction("SystemLogs", "Admin");

        [HttpGet]
        public IActionResult GeneralSettings() => RedirectToAction("GeneralSettings", "Admin");

        // --- เชื่อมโยงไปยัง AccountantController (ระบบฝ่ายบัญชีและการเงิน) ---
        [HttpGet]
        public IActionResult RevenueDashboard() => RedirectToAction("RevenueDashboard", "Accountant");

        [HttpGet]
        public IActionResult PendingPayments() => RedirectToAction("PendingPayments", "Accountant");

        [HttpGet]
        public IActionResult Refunds() => RedirectToAction("Refunds", "Accountant");
    }
}
