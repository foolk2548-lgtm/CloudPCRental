using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CloudPCRental.Models;

namespace CloudPCRental.Controllers
{
    /// <summary>
    /// โมดูลระบบการจองเครื่อง, สล็อตเวลา, การชำระเงิน และประวัติการจองของผู้ใช้ (Booking & Payment Workflow)
    /// </summary>
    public partial class CloudPCController : Controller
    {
        // ==========================================
        // 3. กำหนดเวลาจองของคุณ (Booking) - Wireframe Page 4
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Booking(int? machineId)
        {
            int id = machineId ?? 1;
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อดำเนินการจองเครื่องคอมพิวเตอร์";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Booking", "CloudPC", new { machineId = id }) });
            }

            var machine = await GetMachineByIdAsync(id);
            if (machine == null)
            {
                TempData["ErrorMessage"] = "ไม่พบข้อมูลเครื่องคอมพิวเตอร์ที่ต้องการจอง";
                return RedirectToAction("Machines");
            }

            // ตรวจสอบความพร้อมของเครื่อง: ไม่อนุญาตเฉพาะเครื่องที่อยู่ระหว่างปิดซ่อมหรือรออนุมัติซ่อม
            if (machine.Status == "Maintenance" || machine.Status == "PendingRepair")
            {
                string statusTh = machine.Status == "PendingRepair" ? "รออนุมัติซ่อมบำรุง" : "อยู่ระหว่างการปิดซ่อมบำรุง";
                TempData["ErrorMessage"] = $"ขออภัยครับ เครื่อง {machine.MachineName} (PC-{machine.MachineId:D3}) ขณะนี้ '{statusTh}' ไม่พร้อมให้บริการ กรุณาเลือกเครื่องอื่นครับ";
                return RedirectToAction("Machines");
            }

            int bookingUserId = HttpContext.Session.GetInt32("UserId") ?? 5;
            decimal userBalance = MockDataStore.GetUserBalance(bookingUserId);
            ViewBag.UserBalance = userBalance;
            HttpContext.Session.SetString("WalletBalance", userBalance.ToString("F2"));

            // ส่งข้อมูลช่วงเวลาที่ถูกจองแล้วของเครื่องนี้ เพื่อให้หน้า UI แสดงเต็มเฉพาะเวลาที่ถูกจองจริง
            string selectedDate = MockDataStore.GetTodayThaiDate();

            // ซิงค์การจองจากฐานข้อมูล MySQL (ถ้ามี) เข้าสู่ระบบสล็อตจอง
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbBookings = await _context.Bookings
                        .Where(b => b.MachineId == id && b.PaymentStatus != "Refunded" && b.PaymentStatus != "Rejected")
                        .ToListAsync();
                    foreach (var db in dbBookings)
                    {
                        if (!string.IsNullOrEmpty(db.Note))
                        {
                            MockDataStore.SyncDbBookingSlot(db.BookingId, db.UserId, db.MachineId, db.Note);
                        }
                    }
                }
            }
            catch { }

            ViewBag.BookedSlots = MockDataStore.GetBookedSlots(id, selectedDate);
            ViewBag.StandardSlots = MockDataStore.StandardDailySlots;
            ViewBag.SelectedDate = selectedDate;

            return View(machine);
        }

        // Action คืนข้อมูลช่วงเวลาที่ถูกจองแล้วของเครื่องผ่าน AJAX ตามวันที่ระบุ
        [HttpGet]
        public async Task<IActionResult> GetBookedSlots(int machineId, string? date = null)
        {
            string dateStr = string.IsNullOrWhiteSpace(date) ? MockDataStore.GetTodayThaiDate() : date;

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbBookings = await _context.Bookings
                        .Where(b => b.MachineId == machineId && b.PaymentStatus != "Refunded" && b.PaymentStatus != "Rejected")
                        .ToListAsync();
                    foreach (var db in dbBookings)
                    {
                        if (!string.IsNullOrEmpty(db.Note))
                        {
                            MockDataStore.SyncDbBookingSlot(db.BookingId, db.UserId, db.MachineId, db.Note);
                        }
                    }
                }
            }
            catch { }

            var booked = MockDataStore.GetBookedSlots(machineId, dateStr);
            return Json(new { success = true, bookedSlots = booked, allSlots = MockDataStore.StandardDailySlots, date = dateStr });
        }

        // ==========================================
        // 4. เลือกช่องทางบริการชำระเงิน (Payment) - Wireframe Page 5
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Payment(int machineId, int hours = 2, string? bookingDate = null, string? timeRange = null, string? promoCode = null, string? slots = null)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อดำเนินการชำระเงิน";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Payment", "CloudPC", new { machineId, hours, bookingDate, timeRange, promoCode, slots }) });
            }

            var machine = await GetMachineByIdAsync(machineId);
            if (machine == null) return RedirectToAction("Machines");

            // ตรวจสอบสถานะเครื่องก่อนอนุญาตให้เข้าสู่ขั้นตอนชำระเงิน (ไม่อนุญาตเฉพาะเครื่องที่ซ่อมบำรุง)
            if (machine.Status == "Maintenance" || machine.Status == "PendingRepair")
            {
                string statusTh = machine.Status == "PendingRepair" ? "รออนุมัติซ่อมบำรุง" : "อยู่ระหว่างการปิดซ่อมบำรุง";
                TempData["ErrorMessage"] = $"เครื่อง {machine.MachineName} ขณะนี้ '{statusTh}' ไม่สามารถเข้าสู่ขั้นตอนการชำระเงินได้";
                return RedirectToAction("Machines");
            }

            // 1. คำนวณราคาเต็มก่อนหักส่วนลด (Base Price)
            decimal originalPrice = machine.PricePerHour * hours;

            // 2. ระบบคำนวณโปรโมชั่น 5 เงื่อนไข
            var eligiblePromotions = new List<(string Name, decimal Discount)>();

            // เงื่อนไขที่ 1: โปรโมชั่นลูกค้าใหม่ (ลด 15%) - ตรวจสอบว่าไม่เคยมีประวัติการจองในระบบ
            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            bool hasPreviousBookings = false;
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    hasPreviousBookings = await _context.Bookings.AnyAsync(b => b.UserId == currentUserId);
                }
            }
            catch { }
            if (!hasPreviousBookings)
            {
                hasPreviousBookings = MockDataStore.GetBookings().Any(b => b.UserId == currentUserId);
            }
            if (!hasPreviousBookings && currentUserId > 0)
            {
                decimal disc1 = Math.Round(originalPrice * 0.15m, 2);
                eligiblePromotions.Add(("โปรโมชั่นลูกค้าใหม่ต้อนรับสมาชิก (ลด 15%)", disc1));
            }

            // เงื่อนไขที่ 2: โปรโมชั่นเหมาชั่วโมง (จองตั้งแต่ 5 ชั่วโมงขึ้นไป ลด 10%)
            if (hours >= 5)
            {
                decimal disc2 = Math.Round(originalPrice * 0.10m, 2);
                eligiblePromotions.Add(("โปรโมชั่นเหมาชั่วโมง 5 ชม. ขึ้นไป (ลด 10%)", disc2));
            }

            // เงื่อนไขที่ 3: โปรโมชั่นช่วงดึก / Night Owl (เวลาจองอยู่ในช่วง 00:00 - 06:00 น. ลด 50 บาท)
            if (!string.IsNullOrEmpty(timeRange) && (timeRange.Contains("00:") || timeRange.Contains("01:") || timeRange.Contains("02:") || timeRange.Contains("03:") || timeRange.Contains("04:") || timeRange.Contains("05:") || timeRange.Contains("06:")))
            {
                decimal disc3 = Math.Min(originalPrice, 50.00m);
                eligiblePromotions.Add(("โปรโมชั่นรอบดึก Night Owl 00:00 - 06:00 น. (ลด 50 บาท)", disc3));
            }

            // เงื่อนไขที่ 4: โปรโมชั่นสเปกแรงจัดเต็ม (GPU รุ่น RTX 4090 และเช่า 3 ชม. ขึ้นไป ลด 100 บาท)
            bool isHighSpec = (machine.GPU?.Contains("4090", StringComparison.OrdinalIgnoreCase) == true) ||
                              (machine.MachineName.Contains("4090", StringComparison.OrdinalIgnoreCase) || machine.MachineName.Contains("PRO", StringComparison.OrdinalIgnoreCase));
            if (isHighSpec && hours >= 3)
            {
                decimal disc4 = Math.Min(originalPrice, 100.00m);
                eligiblePromotions.Add(("โปรโมชั่นสเปกแรงจัดเต็ม RTX 4090 เช่า 3 ชม. ขึ้นไป (ลด 100 บาท)", disc4));
            }

            // เงื่อนไขที่ 5: โค้ดส่วนลดพิเศษ (กรอกโค้ด SPU69 ลด 20% สูงสุดไม่เกิน 200 บาท)
            if (!string.IsNullOrWhiteSpace(promoCode) && promoCode.Trim().Equals("SPU69", StringComparison.OrdinalIgnoreCase))
            {
                decimal disc5 = Math.Min(200.00m, Math.Round(originalPrice * 0.20m, 2));
                eligiblePromotions.Add(("โค้ดส่วนลดพิเศษ SPU69 (ลด 20% สูงสุดไม่เกิน 200 บาท)", disc5));
            }

            // คัดเลือกโปรโมชั่นที่ให้ส่วนลดคุ้มค่าที่สุดแก่ลูกค้า (Best Promotion)
            string promotionName = "ไม่มีส่วนลดโปรโมชั่น";
            decimal discount = 0;

            if (eligiblePromotions.Any())
            {
                var bestPromo = eligiblePromotions.OrderByDescending(p => p.Discount).First();
                promotionName = bestPromo.Name;
                discount = bestPromo.Discount;
            }

            // ป้องกันส่วนลดเกินราคาจริง และคำนวณยอดสุทธิ
            discount = Math.Min(discount, originalPrice);
            decimal totalAmount = Math.Max(0, originalPrice - discount);

            ViewBag.Hours = hours;
            ViewBag.BookingDate = string.IsNullOrWhiteSpace(bookingDate) ? MockDataStore.GetTodayThaiDate() : bookingDate;
            ViewBag.TimeRange = string.IsNullOrWhiteSpace(timeRange) ? "09:00 - 11:00" : timeRange;
            ViewBag.OriginalPrice = originalPrice;
            ViewBag.Discount = discount;
            ViewBag.PromotionName = promotionName;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.PromoCode = promoCode;
            ViewBag.Slots = slots;
            decimal currentBal = MockDataStore.GetUserBalance(currentUserId > 0 ? currentUserId : 5);
            ViewBag.UserBalance = currentBal;
            HttpContext.Session.SetString("WalletBalance", currentBal.ToString("F2"));

            return View(machine);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int machineId, string? cardName = null, string? cardNumber = null, int hours = 2, string? bookingDate = null, string? timeRange = null, string? payMethod = "pm-wallet", decimal? totalAmount = null, string? slots = null, IFormFile? slipFile = null, string? sampleSlipUrl = null)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อดำเนินการชำระเงิน";
                return RedirectToAction("Login");
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            var machine = await GetMachineByIdAsync(machineId);
            if (machine == null || machine.Status == "Maintenance" || machine.Status == "PendingRepair")
            {
                TempData["ErrorMessage"] = "ขออภัยครับ เครื่องคอมพิวเตอร์นี้ไม่พร้อมให้บริการ (ปิดซ่อมบำรุง) กรุณาเลือกเครื่องอื่น";
                return RedirectToAction("Machines");
            }
            decimal priceToPay = totalAmount ?? (machine.PricePerHour * hours);
            decimal userBal = MockDataStore.GetUserBalance(userId);

            // กำหนดวันที่และช่วงเวลาที่จะจอง
            string dateToCheck = string.IsNullOrWhiteSpace(bookingDate) ? MockDataStore.GetTodayThaiDate() : bookingDate;
            string timeRangeToCheck = string.IsNullOrWhiteSpace(timeRange) ? "09:00 - 11:00" : timeRange;

            // ตรวจสอบการจองทับซ้อน (Overlap Prevention): หากช่วงเวลาที่เลือกในวันที่ดังกล่าวถูกจองไปแล้ว ไม่อนุญาตให้จองซ้ำ
            var currentBookedSlots = MockDataStore.GetBookedSlots(machineId, dateToCheck);
            List<string> requestedSlots;
            if (!string.IsNullOrWhiteSpace(slots))
            {
                requestedSlots = MockDataStore.ParseSlotsFromTimeRange(slots);
            }
            else
            {
                requestedSlots = MockDataStore.ParseSlotsFromTimeRange(timeRangeToCheck);
            }

            if (!requestedSlots.Any() && !string.IsNullOrWhiteSpace(timeRangeToCheck))
            {
                requestedSlots = MockDataStore.ParseSlotsFromTimeRange(timeRangeToCheck);
            }

            bool isSlotConflict = requestedSlots.Any(s => currentBookedSlots.Contains(s));
            if (isSlotConflict)
            {
                var conflicted = requestedSlots.Where(s => currentBookedSlots.Contains(s)).ToList();
                string conflictDetails = string.Join(", ", conflicted);
                TempData["ErrorMessage"] = $"ขออภัยครับ ช่วงเวลาที่คุณเลือก ({conflictDetails}) บนเครื่องนี้ในวันที่ {dateToCheck} มีผู้ใช้งานท่านอื่นจองไปแล้ว ไม่สามารถทำการจองทับซ้อนได้ กรุณาเลือกช่วงเวลาว่างอื่นหรือเลือกวันอื่นครับ";
                return RedirectToAction("Booking", new { machineId });
            }

            string paymentStatus = "Approved";
            string? slipUrl = null;

            // 1. กรณีชำระผ่านกระเป๋าเงิน CloudPC Wallet (ตัดเครดิตทันที)
            if (payMethod == "pm-wallet" || payMethod == "wallet" || string.IsNullOrWhiteSpace(payMethod))
            {
                if (userBal < priceToPay)
                {
                    TempData["ErrorMessage"] = $"ยอดเงินคงเหลือในกระเป๋าเงินไม่เพียงพอกับค่าใช้จ่าย! (มี {userBal:N2} THB แต่ต้องชำระ {priceToPay:N2} THB ขาดอีก {(priceToPay - userBal):N2} THB) คุณจะต้องมีเงินในกระเป๋าเงินเพียงพอก่อน หรือเลือกชำระผ่าน พร้อมเพย์ QR Code แนบสลิปครับ";
                    return RedirectToAction("Payment", new { machineId, hours, bookingDate, timeRange, slots });
                }

                MockDataStore.DeductBalance(userId, priceToPay, $"ชำระค่าเช่า {machine?.MachineName ?? "Cloud PC"} ({hours} ชม.)", $"#CPC-{machineId:D3}");
                userBal = MockDataStore.GetUserBalance(userId);
                HttpContext.Session.SetString("WalletBalance", userBal.ToString("F2"));
                paymentStatus = "Approved";
                MockDataStore.UpdateMachineStatus(machineId, "Rented");
            }
            // 2. กรณีชำระผ่านพร้อมเพย์ QR Code (PromptPay) -> ส่งสลิปให้ฝ่ายบัญชีตรวจสอบ (Payment Verification)
            else if (payMethod == "pm-qr")
            {
                paymentStatus = "Pending";

                // จัดการไฟล์สลิปที่ลูกค้าอัปโหลด
                if (slipFile != null && slipFile.Length > 0)
                {
                    try
                    {
                        string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "slips");
                        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);
                        string fileName = $"slip_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..6]}{Path.GetExtension(slipFile.FileName)}";
                        string fullPath = Path.Combine(uploadsDir, fileName);
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await slipFile.CopyToAsync(stream);
                        }
                        slipUrl = $"/uploads/slips/{fileName}";
                    }
                    catch
                    {
                        slipUrl = !string.IsNullOrWhiteSpace(sampleSlipUrl) ? sampleSlipUrl : "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=600&auto=format&fit=crop&q=80";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(sampleSlipUrl))
                {
                    slipUrl = sampleSlipUrl;
                }
                else
                {
                    slipUrl = "https://images.unsplash.com/photo-1554224155-6726b3ff858f?w=600&auto=format&fit=crop&q=80";
                }
            }
            // 3. กรณีชำระผ่านบัตรเครดิต/เดบิต
            else if (payMethod == "pm-card")
            {
                paymentStatus = "Approved";
                MockDataStore.UpdateMachineStatus(machineId, "Rented");
            }

            // บันทึกรายการจองใหม่ใน MockDataStore ทันที พร้อมล็อกสล็อตเวลาบนวันที่ระบุ
            string bookingTimeNote = $"{dateToCheck}, {timeRangeToCheck} ({hours} ชม.)";
            var newBooking = MockDataStore.CreateNewBooking(userId, machineId, hours, priceToPay, bookingTimeNote, dateToCheck, timeRangeToCheck, requestedSlots, paymentStatus, slipUrl);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    if (paymentStatus == "Approved")
                    {
                        var m = await _context.Machines.FindAsync(machineId);
                        if (m != null) m.Status = "Rented";
                    }

                    var dbBooking = new Booking
                    {
                        UserId = userId,
                        MachineId = machineId,
                        RentalHours = hours,
                        TotalAmount = priceToPay,
                        PaymentDate = DateTime.Now,
                        PaymentStatus = paymentStatus,
                        SlipImageUrl = slipUrl,
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now.AddHours(hours),
                        Note = bookingTimeNote
                    };
                    _context.Bookings.Add(dbBooking);
                    await _context.SaveChangesAsync();
                }
            }
            catch { }

            int earnedPoints = 0;
            if (paymentStatus == "Approved")
            {
                earnedPoints = MockDataStore.AwardBookingPoints(userId, priceToPay, machine?.MachineName ?? "Cloud PC", hours, newBooking.BookingId);
            }

            if (paymentStatus == "Pending")
            {
                TempData["SuccessMessage"] = $"ส่งสลิปโอนเงินรายการ #{newBooking.BookingId} (ยอด {priceToPay:N2} ฿) ให้ฝ่ายบัญชีเรียบร้อยแล้ว! ฝ่ายบัญชีกำลังตรวจสอบความถูกต้อง เมื่ออนุมัติแล้วคุณจะสามารถใช้งานเครื่องและได้รับคะแนนสะสมทันทีครับ";
                return RedirectToAction("MyBookings");
            }
            else
            {
                TempData["SuccessMessage"] = $"ชำระเงินเรียบร้อยแล้ว! (หักค่าบริการ {priceToPay:N2} THB) คุณได้รับคะแนนสะสม +{earnedPoints} PTS ยอดคงเหลือในกระเป๋า: {userBal:N2} THB พร้อมเข้าใช้งานเครื่องทันที";
                return RedirectToAction("PaymentSuccess", new { machineId, hours, bookingDate, timeRange, payMethod, paidAmount = priceToPay, remainingBal = userBal, earnedPoints });
            }
        }

        // ==========================================
        // 5. ชำระเงินสำเร็จแล้ว (Payment Success / Receipt) - Wireframe Page 6
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int? machineId, int hours = 2, string? bookingDate = null, string? timeRange = null, string? payMethod = null, decimal? paidAmount = null, decimal? remainingBal = null, int earnedPoints = 0)
        {
            int id = machineId ?? 1;
            var machine = await GetMachineByIdAsync(id);
            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;

            ViewBag.CustomerName = HttpContext.Session.GetString("Username") ?? "ธนพล ดีสมเกียรติ";
            ViewBag.CustomerEmail = HttpContext.Session.GetString("Email") ?? "thanapol@example.com";
            ViewBag.TxnNumber = "TXN-" + DateTime.Now.ToString("yyyyMMdd") + "-9812";
            ViewBag.TxnDate = MockDataStore.GetCurrentThaiDateTime();
            ViewBag.Hours = hours;
            ViewBag.BookingDate = string.IsNullOrWhiteSpace(bookingDate) ? MockDataStore.GetTodayThaiDate() : bookingDate;
            ViewBag.TimeRange = string.IsNullOrWhiteSpace(timeRange) ? "(09:00 - 11:00 น.)" : $"({timeRange} น.)";
            ViewBag.PayMethod = payMethod ?? "pm-wallet";
            ViewBag.PaidAmount = paidAmount ?? (machine?.PricePerHour * hours ?? 30.00m);
            ViewBag.RemainingBal = remainingBal ?? MockDataStore.GetUserBalance(userId);
            ViewBag.EarnedPoints = earnedPoints > 0 ? earnedPoints : Math.Max(1, (int)Math.Round((double)(ViewBag.PaidAmount) / 10.0 * 1.5));

            return View(machine);
        }

        // ==========================================
        // 6. เปลี่ยนทางจากรีโมทไปยังหน้ารายการการจองของฉัน
        // ==========================================
        [HttpGet]
        public IActionResult RemoteDesktop(int? machineId)
        {
            TempData["SuccessMessage"] = "คุณสามารถตรวจสอบสถานะและสั่งการคอมพิวเตอร์ของคุณได้ที่แผงควบคุมนี้";
            return RedirectToAction("MyBookings");
        }

        // ==========================================
        // 7. แดชบอร์ดลูกค้า: ประวัติการจองของฉัน - Wireframe Page 7
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อดูประวัติการจองและจัดการเครื่องของคุณ";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyBookings", "CloudPC") });
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 5;
            var bookings = await GetAllBookingsAsync();
            var now = DateTime.Now;

            // ดึงเฉพาะการจองของ user คนนี้เท่านั้น (บัญชีใครบัญชีมัน)
            var myBookings = bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingId)
                .ToList();

            // อัปเดตสถานะตามเวลาจริง: ถ้าหมดเวลาแล้วให้เป็น Completed
            foreach (var b in myBookings)
            {
                if ((b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)") && b.EndTime.HasValue)
                {
                    if (now >= b.EndTime.Value)
                        b.PaymentStatus = "Completed";
                    else if (now >= b.StartTime.GetValueOrDefault() && now < b.EndTime.Value)
                        b.PaymentStatus = "InUse"; // กำลังใช้งานอยู่
                }
            }

            ViewBag.NewBookings = myBookings;
            ViewBag.ServerNow = now; // ส่งเวลา server ให้ View ใช้คำนวณ countdown

            // คำนวณสถิติจากข้อมูลจริงของ user คนนี้เท่านั้น
            int activeMachines = myBookings.Count(b => b.PaymentStatus == "Approved" || b.PaymentStatus == "จอง (พร้อมใช้งาน)" || b.PaymentStatus == "InUse");
            ViewBag.ActiveMachinesCount = activeMachines;
            ViewBag.TotalBookingsCount = myBookings.Count;
            ViewBag.AccumulatedHoursCount = myBookings.Sum(b => b.RentalHours);

            // ตรวจสอบสถานะคำขอคืนเงินของ user นี้ (ถ้ามี)
            var refundedBooking = myBookings.FirstOrDefault(b => b.PaymentStatus == "Refunded");
            var pendingRefundBooking = myBookings.FirstOrDefault(b => b.PaymentStatus == "RefundRequested");
            var rejectedRefundBooking = myBookings.FirstOrDefault(b => b.PaymentStatus == "RefundRejected");
            var activeBooking = refundedBooking ?? pendingRefundBooking ?? rejectedRefundBooking;

            ViewBag.Booking94812Status = activeBooking?.PaymentStatus ?? "";
            ViewBag.Booking94812Reason = activeBooking?.RefundReason ?? "";
            ViewBag.Booking94812Note = activeBooking?.Note ?? "";

            decimal currentBal = MockDataStore.GetUserBalance(userId);
            ViewBag.CurrentBalance = currentBal.ToString("N2");
            HttpContext.Session.SetString("WalletBalance", currentBal.ToString("F2"));

            return View();
        }
    }
}
