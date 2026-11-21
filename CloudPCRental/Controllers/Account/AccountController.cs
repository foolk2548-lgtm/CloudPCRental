using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// คอนโทรลเลอร์สำหรับระบบสมาชิกและการเข้าสู่ระบบ (Authentication & Account Management)
    /// รับผิดชอบ: เข้าสู่ระบบ (Login พร้อม Session Clear), สมัครสมาชิก (Register ป้องกันอีเมลซ้ำ),
    /// ลืมรหัสผ่าน (ForgotPassword), ออกจากระบบ (Logout), และตั้งค่าบัญชี (Profile Settings)
    /// </summary>
    public class AccountController : Controller
    {
        private readonly Csi402dbContext _context;

        public AccountController(Csi402dbContext context)
        {
            _context = context;
        }

        // ==========================================
        // Helper Methods: ตรวจสอบและดึงข้อมูลผู้ใช้
        // ==========================================
        private bool IsLoggedIn() => HttpContext.Session.GetInt32("UserId") != null;
        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin" || HttpContext.Session.GetString("Role") == "Manager";

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
        // 1. เข้าสู่ระบบ (Login)
        // ==========================================
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (IsLoggedIn())
            {
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                var userRole = HttpContext.Session.GetString("Role");
                if (userRole == "Accountant") return RedirectToAction("RevenueDashboard", "Accountant");
                if (userRole == "Admin" || userRole == "Staff") return RedirectToAction("AdminDashboard", "Admin");
                if (userRole == "Manager") return RedirectToAction("Dashboard", "Manager");
                return RedirectToAction("Index", "CloudPC");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(model);

            var user = await AuthenticateUserAsync(model.UsernameOrEmail, model.Password);

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            if (user == null)
            {
                MockDataStore.LogActivity(model.UsernameOrEmail, "Login", $"พยายามเข้าสู่ระบบไม่สำเร็จสำหรับบัญชี '{model.UsernameOrEmail}'", "สำคัญ", clientIp);
                ModelState.AddModelError(string.Empty, "ชื่อผู้ใช้ / อีเมล หรือรหัสผ่านไม่ถูกต้อง (หากบัญชีถูกลบออกจากระบบ กรุณาสมัครสมาชิกใหม่)");
                return View(model);
            }

            MockDataStore.LogActivity(user.Username, "Login", $"ผู้ใช้งาน {user.Username} (บทบาท: {user.Role}) เข้าสู่ระบบสำเร็จ", "ข้อมูลทั่วไป", clientIp);

            // ล้างค่า Session เก่าที่อาจตกค้างก่อนบันทึกค่าใหม่
            HttpContext.Session.Clear();

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role);

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("CloudPC_UserId", user.UserId.ToString(), cookieOptions);
            Response.Cookies.Append("CloudPC_Username", user.Username, cookieOptions);
            Response.Cookies.Append("CloudPC_Role", user.Role, cookieOptions);

            decimal currentBal = MockDataStore.GetUserBalance(user.UserId);
            HttpContext.Session.SetString("WalletBalance", currentBal.ToString("F2"));

            TempData["SuccessMessage"] = $"เข้าสู่ระบบสำเร็จ! ยินดีต้อนรับคุณ {user.Username}";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "Accountant")
            {
                return RedirectToAction("RevenueDashboard", "Accountant");
            }
            if (user.Role == "Manager")
            {
                return RedirectToAction("Dashboard", "Manager");
            }
            if (user.Role == "Admin" || user.Role == "Staff")
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }
            return RedirectToAction("Index", "CloudPC");
        }

        // เข้าสู่ระบบด่วนสำหรับทดสอบระบบตามบทบาท (Roles) ทั้ง 5 ตำแหน่ง
        [HttpGet]
        public async Task<IActionResult> QuickLogin(string? returnUrl = null, string accountType = "user")
        {
            var allUsers = await GetAllUsersAsync();
            User? user = accountType.ToLower() switch
            {
                "admin" => allUsers.FirstOrDefault(u => u.Role == "Admin") ?? new User
                {
                    UserId = 1,
                    Username = "admin (สมชาย)",
                    Email = "admin@cloudpc.com",
                    Role = "Admin"
                },
                "manager" => allUsers.FirstOrDefault(u => u.Role == "Manager") ?? new User
                {
                    UserId = 2,
                    Username = "manager (วีระ)",
                    Email = "manager@cloudpc.com",
                    Role = "Manager"
                },
                "staff" => allUsers.FirstOrDefault(u => u.Role == "Staff") ?? new User
                {
                    UserId = 3,
                    Username = "staff (กานดา)",
                    Email = "staff@cloudpc.com",
                    Role = "Staff"
                },
                "accountant" => allUsers.FirstOrDefault(u => u.Role == "Accountant") ?? new User
                {
                    UserId = 4,
                    Username = "accountant (ปรียา)",
                    Email = "account@cloudpc.com",
                    Role = "Accountant"
                },
                _ => allUsers.FirstOrDefault(u => u.Role == "User") ?? new User
                {
                    UserId = 5,
                    Username = "ธนพล ดีสมเกียรติ",
                    Email = "thanapol@example.com",
                    Role = "User"
                }
            };

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("Email", user.Email);
            HttpContext.Session.SetString("Role", user.Role);

            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("CloudPC_UserId", user.UserId.ToString(), cookieOptions);
            Response.Cookies.Append("CloudPC_Username", user.Username, cookieOptions);
            Response.Cookies.Append("CloudPC_Role", user.Role, cookieOptions);

            decimal currentBal = MockDataStore.GetUserBalance(user.UserId);
            HttpContext.Session.SetString("WalletBalance", currentBal.ToString("F2"));

            TempData["SuccessMessage"] = $"เข้าสู่ระบบสำเร็จในบทบาท {user.Role} ({user.Username})!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (user.Role == "Accountant")
            {
                return RedirectToAction("RevenueDashboard", "Accountant");
            }
            if (user.Role == "Manager")
            {
                return RedirectToAction("Dashboard", "Manager");
            }
            if (user.Role == "Admin" || user.Role == "Staff")
            {
                return RedirectToAction("AdminDashboard", "Admin");
            }
            return RedirectToAction("Index", "CloudPC");
        }

        // ==========================================
        // 2. สมัครสมาชิก (Register)
        // ==========================================
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ตรวจสอบว่ามี Email นี้ในฐานข้อมูลแล้วหรือยัง (AnyAsync) ป้องกันการสมัครซ้ำ
            bool emailExists = false;
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == model.Email.ToLower());
                }
            }
            catch { }

            // ตรวจสอบใน MockDataStore ด้วยกรณีเซิร์ฟเวอร์ MySQL ออฟไลน์
            if (!emailExists)
            {
                emailExists = MockDataStore.GetUsers().Any(u => u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
            }

            if (emailExists)
            {
                ModelState.AddModelError("Email", "อีเมลนี้ถูกใช้งานในระบบแล้ว กรุณาใช้อีเมลอื่นหรือเข้าสู่ระบบ");
                return View(model);
            }

            var newUser = new User
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password,
                Role = "User"
            };

            MockDataStore.AddUser(newUser);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();
                    MockDataStore.SetUserBalance(newUser.UserId, 0.00m);
                    MockDataStore.SetUserPoints(newUser.UserId, 0);
                }
            }
            catch { }

            TempData["SuccessMessage"] = "สมัครสมาชิกสำเร็จเรียบร้อย! สามารถเข้าสู่ระบบได้ทันที";
            return RedirectToAction("Login");
        }

        // ==========================================
        // 3. ลืมรหัสผ่าน (Forgot Password)
        // ==========================================
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = (await GetAllUsersAsync()).FirstOrDefault(u => u.Email.Equals(model.Email, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                ModelState.AddModelError("Email", "ไม่พบข้อมูลบัญชีที่ผูกกับอีเมลนี้");
                return View(model);
            }

            user.Password = model.NewPassword;
            MockDataStore.UpdateUser(user);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                    if (dbUser != null)
                    {
                        dbUser.Password = model.NewPassword;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = "รีเซ็ตรหัสผ่านใหม่สำเร็จแล้ว! กรุณาเข้าสู่ระบบด้วยรหัสผ่านใหม่";
            return RedirectToAction("Login");
        }

        // ==========================================
        // 4. ออกจากระบบ (Logout)
        // ==========================================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("CloudPC_UserId");
            Response.Cookies.Delete("CloudPC_Username");
            Response.Cookies.Delete("CloudPC_Role");
            TempData["SuccessMessage"] = "ออกจากระบบเรียบร้อยแล้ว";
            return RedirectToAction("Login");
        }

        // ==========================================
        // 5. ตั้งค่าบัญชี (Account Settings / Profile)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> AccountSettings()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อตั้งค่าบัญชีของคุณ";
                return RedirectToAction("Login", new { returnUrl = Url.Action("AccountSettings", "Account") });
            }

            var username = HttpContext.Session.GetString("Username") ?? "ธนพล ดีสมเกียรติ";
            var email = HttpContext.Session.GetString("Email") ?? "thanapol@example.com";
            var userId = HttpContext.Session.GetInt32("UserId") ?? 5;

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
                        user.Username = model.FullName;
                        user.Email = model.Email;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            TempData["SuccessMessage"] = "อัปเดตข้อมูลบัญชีของคุณสำเร็จเรียบร้อยแล้ว";
            return RedirectToAction("AccountSettings");
        }
    }
}

