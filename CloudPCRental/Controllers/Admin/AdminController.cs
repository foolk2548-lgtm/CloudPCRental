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
                    var mockAll = MockDataStore.GetMachines();

                    // หากใน Database มีเครื่องไม่ครบ 22 เครื่อง ให้ซิงค์เครื่องที่ขาดเข้าไปใน Database อัตโนมัติ
                    if (dbList.Count < mockAll.Count)
                    {
                        foreach (var mm in mockAll)
                        {
                            if (!dbList.Any(d => d.MachineId == mm.MachineId || d.MachineName.Equals(mm.MachineName, StringComparison.OrdinalIgnoreCase)))
                            {
                                _context.Machines.Add(new Machine
                                {
                                    MachineName = mm.MachineName,
                                    CPU = mm.CPU,
                                    RAM = mm.RAM,
                                    GPU = mm.GPU,
                                    PricePerHour = mm.PricePerHour,
                                    Status = mm.Status
                                });
                            }
                        }
                        await _context.SaveChangesAsync();
                        dbList = await _context.Machines.ToListAsync();
                    }

                    list = dbList.Any() ? dbList : mockAll;
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
                    var mockUsers = MockDataStore.GetUsers();

                    // ซิงค์ลบผู้ใช้ใน DB ที่ถูกแอดมินลบออกไปแล้วในระบบจริง (เช่น thanapol, user1)
                    var mockUsernames = mockUsers.Select(u => u.Username.ToLower()).ToHashSet();
                    var obsoleteUsers = dbUsers.Where(u => !mockUsernames.Contains(u.Username.ToLower())).ToList();
                    if (obsoleteUsers.Any())
                    {
                        _context.Users.RemoveRange(obsoleteUsers);
                        await _context.SaveChangesAsync();
                        dbUsers = await _context.Users.ToListAsync();
                    }

                    // ซิงค์ผู้ใช้จาก MockDataStore เข้า DB หากยังไม่มี
                    var dbUsernames = dbUsers.Select(u => u.Username.ToLower()).ToHashSet();
                    bool added = false;
                    foreach (var mu in mockUsers)
                    {
                        if (!dbUsernames.Contains(mu.Username.ToLower()))
                        {
                            _context.Users.Add(new User
                            {
                                Username = mu.Username,
                                Email = mu.Email,
                                Password = mu.Password,
                                Role = mu.Role
                            });
                            added = true;
                        }
                    }
                    if (added)
                    {
                        await _context.SaveChangesAsync();
                        dbUsers = await _context.Users.ToListAsync();
                    }

                    if (dbUsers.Any()) return dbUsers;
                }
            }
            catch { }
            return MockDataStore.GetUsers();
        }

        private async Task<List<Booking>> GetAllBookingsAsync()
        {
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbBookings = await _context.Bookings.Include(b => b.Machine).Include(b => b.User).OrderByDescending(b => b.PaymentDate).ToListAsync();
                    if (dbBookings.Any()) return dbBookings;
                }
            }
            catch { }
            return MockDataStore.GetBookings();
        }

        private async Task<DashboardUsageStats> GetDashboardUsageStatsAsync(List<Machine> machines)
        {
            var stats = new DashboardUsageStats();
            int totalMachines = machines.Count > 0 ? machines.Count : 22;

            List<Booking> allBookings = await GetAllBookingsAsync();
            var bookedSlotsRegistry = MockDataStore.GetBookedSlotsRegistry();
            string todayThai = MockDataStore.GetTodayThaiDate();

            var periods = new[]
            {
                new { Label = "09:00 - 12:00 น.", Hours = new[] { 9, 10, 11 } },
                new { Label = "12:00 - 15:00 น.", Hours = new[] { 12, 13, 14 } },
                new { Label = "15:00 - 18:00 น.", Hours = new[] { 15, 16, 17 } },
                new { Label = "18:00 - 21:00 น.", Hours = new[] { 18, 19, 20 } }
            };

            var rentedMachineIds = machines.Where(m => m.Status == "Rented").Select(m => m.MachineId).ToHashSet();

            foreach (var p in periods)
            {
                var activeMachineIds = new HashSet<int>();

                // ก. ตรวจสอบจาก BookedSlotsRegistry ที่ตรงกับวันนี้
                foreach (var reg in bookedSlotsRegistry)
                {
                    if (MockDataStore.IsSameDate(reg.Date, todayThai))
                    {
                        var parsedSlots = MockDataStore.ParseSlotsFromTimeRange(reg.Slot);
                        foreach (var s in parsedSlots)
                        {
                            if (IsSlotInHours(s, p.Hours))
                            {
                                activeMachineIds.Add(reg.MachineId);
                            }
                        }
                    }
                }

                // ข. ตรวจสอบจาก allBookings ที่ตรงกับวันนี้
                foreach (var b in allBookings)
                {
                    if (b.MachineId > 0 && b.PaymentStatus != "Refunded" && b.PaymentStatus != "Rejected")
                    {
                        bool isToday = b.PaymentDate.Date == DateTime.Today || 
                                       (!string.IsNullOrEmpty(b.Note) && MockDataStore.IsSameDate(MockDataStore.ExtractDateFromNote(b.Note), todayThai));
                        
                        if (isToday && !string.IsNullOrEmpty(b.Note))
                        {
                            var parsedSlots = MockDataStore.ParseSlotsFromTimeRange(b.Note);
                            foreach (var s in parsedSlots)
                            {
                                if (IsSlotInHours(s, p.Hours))
                                {
                                    activeMachineIds.Add(b.MachineId);
                                }
                            }
                        }
                    }
                }

                // ค. เครื่องที่มีสถานะ Rented (กำลังใช้งานอยู่) หากมีสล็อตให้ดึงตามสล็อต หากไม่มีให้เฉลี่ยตามช่วงเวลาทำการ
                foreach (var rId in rentedMachineIds)
                {
                    if (p.Hours.Contains(12) || p.Hours.Contains(15) || p.Hours.Contains(18))
                    {
                        activeMachineIds.Add(rId);
                    }
                    else if (rId % 2 == 0)
                    {
                        activeMachineIds.Add(rId);
                    }
                }

                int count = activeMachineIds.Count;
                int pct = totalMachines > 0 ? (int)Math.Min(100, Math.Round((double)count / totalMachines * 100)) : 0;

                stats.TimeSlots.Add(new TimeSlotUsageItem
                {
                    TimeRange = p.Label,
                    MachineCount = count,
                    Percentage = pct
                });
            }

            // คำนวณประเภทสเปกคอมพิวเตอร์ที่ถูกจองมากสุดตามประวัติการจองจริง
            var categoryCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Gaming Pro (RTX 4080 / RTX 4090)", 0 },
                { "Graphic Studio (Render / 3D)", 0 },
                { "AI Cluster (EPYC / A100)", 0 },
                { "Standard Office (Core i5 / Work)", 0 }
            };

            var machineMap = machines.ToDictionary(m => m.MachineId);

            foreach (var b in allBookings)
            {
                if (b.MachineId > 0 && b.PaymentStatus != "Rejected" && b.PaymentStatus != "RefundRejected")
                {
                    if (machineMap.TryGetValue(b.MachineId, out var m))
                    {
                        string cat = MockDataStore.GetMachineCategory(m);
                        if (categoryCounts.ContainsKey(cat))
                        {
                            categoryCounts[cat]++;
                        }
                        else
                        {
                            categoryCounts[cat] = 1;
                        }
                    }
                }
            }

            var sorted = categoryCounts.OrderByDescending(kvp => kvp.Value).Take(3).ToList();
            int rank = 1;
            foreach (var kvp in sorted)
            {
                stats.PopularSpecs.Add(new PopularSpecItem
                {
                    Rank = rank++,
                    CategoryName = kvp.Key,
                    BookingCount = kvp.Value
                });
            }

            return stats;
        }

        private static bool IsSlotInHours(string slot, int[] targetHours)
        {
            if (string.IsNullOrWhiteSpace(slot)) return false;
            var parts = slot.Split(new[] { '-', '–' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                var startStr = parts[0].Trim().Replace("น.", "");
                if (TimeSpan.TryParse(startStr, out var ts))
                {
                    return targetHours.Contains(ts.Hours);
                }
            }
            return false;
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

            // สถิติการใช้งานตามช่วงเวลาวันนี้ และประเภทสเปกที่ถูกจองมากสุดตามข้อมูลจริง
            ViewBag.UsageStats = await GetDashboardUsageStatsAsync(machines);

            return View(machines);
        }

        // ==========================================
        // 2. บันทึกระบบและการตั้งค่าทั่วไป (System Logs & Settings)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> SystemLogs(DateTime? date = null, string? search = null, string? category = null, string? severity = null)
        {
            if (!IsLoggedIn()) return RedirectToAction("Login", "Account");
            if (!IsAdminOnly())
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงบันทึกระบบ";
                return RedirectToAction("AdminDashboard");
            }

            List<SystemLog> logs;
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var query = _context.SystemLogs.AsQueryable();
                    if (date.HasValue)
                    {
                        var targetDate = date.Value.Date;
                        query = query.Where(l => l.Timestamp.Date == targetDate);
                    }
                    if (!string.IsNullOrWhiteSpace(search))
                    {
                        var s = search.Trim().ToLower();
                        query = query.Where(l => l.PerformedBy.ToLower().Contains(s) || l.Detail.ToLower().Contains(s) || l.IpAddress.ToLower().Contains(s));
                    }
                    if (!string.IsNullOrWhiteSpace(category) && category != "ประเภททั้งหมด")
                    {
                        query = query.Where(l => l.Category == category);
                    }
                    if (!string.IsNullOrWhiteSpace(severity) && severity != "ระดับความรุนแรงทั้งหมด")
                    {
                        query = query.Where(l => l.Severity == severity);
                    }
                    logs = await query.OrderByDescending(l => l.Timestamp).ToListAsync();
                }
                else
                {
                    logs = MockDataStore.GetSystemLogs(date, search, category, severity);
                }
            }
            catch
            {
                logs = MockDataStore.GetSystemLogs(date, search, category, severity);
            }

            ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
            ViewBag.SearchKeyword = search ?? "";
            ViewBag.SelectedCategory = category ?? "ประเภททั้งหมด";
            ViewBag.SelectedSeverity = severity ?? "ระดับความรุนแรงทั้งหมด";

            return View(logs);
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
