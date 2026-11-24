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

            int bookingUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            decimal userBalance = MockDataStore.GetUserBalance(bookingUserId);
            ViewBag.UserBalance = userBalance;
            HttpContext.Session.SetString("WalletBalance", userBalance.ToString("F2"));

            // ส่งข้อมูลช่วงเวลาที่ถูกจองแล้วของเครื่องนี้ เพื่อให้หน้า UI แสดงเต็มเฉพาะเวลาที่ถูกจองจริง
            // หากเวลาปัจจุบันเกิน 23:00 น. (สล็อตทั้งหมดของวันนี้สิ้นสุดแล้ว) ให้เลือกวันพรุ่งนี้เริ่มต้นทันที
            var nowThai = DateTime.Now;
            string selectedDate = (nowThai.Hour >= 23)
                ? MockDataStore.FormatThaiDate(nowThai.AddDays(1))
                : MockDataStore.GetTodayThaiDate();

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

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
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
                        PaymentStatus = newBooking.PaymentStatus,
                        SlipImageUrl = slipUrl,
                        StartTime = newBooking.StartTime ?? DateTime.Now,
                        EndTime = newBooking.EndTime ?? DateTime.Now.AddHours(hours),
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

            string clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string username = HttpContext.Session.GetString("Username") ?? "ลูกค้า";
            MockDataStore.LogActivity(username, "Booking", $"สร้างรายการจอง #{newBooking.BookingId} สำหรับเครื่อง {machine?.MachineName ?? $"PC-{machineId:D3}"} ({hours} ชม. {priceToPay:N2} บาท, สถานะ: {paymentStatus})", "ข้อมูลทั่วไป", clientIp);

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
            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            ViewBag.CustomerName = HttpContext.Session.GetString("Username") ?? "ผู้ใช้งาน";
            ViewBag.CustomerEmail = HttpContext.Session.GetString("Email") ?? "";
            ViewBag.UserId = userId;
            ViewBag.TxnNumber = "REC-" + DateTime.Now.ToString("yyyyMMdd") + "-9812";
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
        // 5.1 ดูและพิมพ์ใบเสร็จรับเงินย้อนหลัง (View / Download Past Receipt)
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Receipt(int id)
        {
            if (!IsLoggedIn())
            {
                TempData["ErrorMessage"] = "กรุณาเข้าสู่ระบบก่อน เพื่อดูใบเสร็จรับเงิน";
                return RedirectToAction("Login", "Account");
            }

            int currentUserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            string currentUserRole = HttpContext.Session.GetString("Role") ?? "User";

            var allBookings = await GetAllBookingsAsync();
            var booking = allBookings.FirstOrDefault(b => b.BookingId == id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = $"ไม่พบข้อมูลรายการจองรหัส #{id}";
                return RedirectToAction("MyBookings");
            }

            if (currentUserRole == "User" && booking.UserId != currentUserId && currentUserId > 0)
            {
                TempData["ErrorMessage"] = "คุณไม่มีสิทธิ์เข้าถึงใบเสร็จของผู้อื่น";
                return RedirectToAction("MyBookings");
            }

            var machine = booking.Machine ?? await GetMachineByIdAsync(booking.MachineId);
            ViewBag.CustomerName = HttpContext.Session.GetString("Username") ?? (booking.User?.Username ?? "ผู้ใช้งาน");
            ViewBag.CustomerEmail = HttpContext.Session.GetString("Email") ?? (booking.User?.Email ?? "");
            ViewBag.UserId = booking.UserId;
            ViewBag.TxnNumber = "REC-" + booking.PaymentDate.ToString("yyyyMMdd") + $"-{booking.BookingId}";
            ViewBag.TxnDate = MockDataStore.FormatThaiDate(booking.PaymentDate) + ", " + booking.PaymentDate.ToString("HH:mm:ss น.");
            ViewBag.Hours = booking.RentalHours > 0 ? booking.RentalHours : 1;
            ViewBag.PaidAmount = booking.TotalAmount;
            ViewBag.BookingDate = MockDataStore.ExtractDateFromNote(booking.Note) ?? MockDataStore.FormatThaiDate(booking.PaymentDate);
            ViewBag.TimeRange = !string.IsNullOrWhiteSpace(booking.Note) && booking.Note.Contains("(")
                ? booking.Note
                : $"({booking.RentalHours} ชม.)";
            ViewBag.PayMethod = "wallet";
            ViewBag.RemainingBal = MockDataStore.GetUserBalance(booking.UserId);
            ViewBag.EarnedPoints = Math.Max(1, (int)Math.Round((double)booking.TotalAmount / 10.0 * 1.5));

            return View("PaymentSuccess", machine);
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

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var bookings = await GetAllBookingsAsync();
            var now = DateTime.Now;

            // ดึงเฉพาะการจองของ user คนนี้เท่านั้น (บัญชีใครบัญชีมัน)
            var myBookings = bookings
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingId)
                .ToList();

            // อัปเดตสถานะตามเวลาจริง: ถ้าหมดเวลาแล้วให้เป็น Completed และทำความสะอาดชื่อเครื่องภาษาไทย
            var thCulture = new System.Globalization.CultureInfo("th-TH");
            foreach (var b in myBookings)
            {
                if (b.Machine != null && MockDataStore.StandardMachineNames.TryGetValue(b.Machine.MachineId, out var sn))
                {
                    b.Machine.MachineName = sn;
                }
                else if (b.MachineId > 0 && MockDataStore.StandardMachineNames.TryGetValue(b.MachineId, out var sn2))
                {
                    if (b.Machine == null) b.Machine = MockDataStore.GetMachineById(b.MachineId);
                    if (b.Machine != null) b.Machine.MachineName = sn2;
                }

                if (!string.IsNullOrEmpty(b.Note) && (b.Note.Contains("à") || b.Note.Contains("¹") || b.Note.Contains("¸")))
                {
                    if (b.MachineId == 0 || b.Note.Contains("Wallet") || b.Note.Contains("เติมเงิน"))
                    {
                        b.Note = $"{b.PaymentDate.ToString("d MMMM yyyy, HH:mm น.", thCulture)} (เติมเงินเข้ากระเป๋าเงิน CloudPC Wallet)";
                    }
                    else
                    {
                        b.Note = $"{b.PaymentDate.ToString("d MMMM yyyy, HH:mm", thCulture)} - {b.PaymentDate.AddHours(b.RentalHours):HH:mm น.} ({b.RentalHours} ชม.)";
                    }
                }

                if (!string.IsNullOrEmpty(b.RefundReason) && (b.RefundReason.Contains("à") || b.RefundReason.Contains("¹") || b.RefundReason.Contains("¸")))
                {
                    b.RefundReason = "เครื่องมีปัญหาจอฟ้าและกระตุก ไม่สามารถเรนเดอร์งานกราฟิกต่อได้";
                }

                // ดึงวันเวลาเริ่มและสิ้นสุดจริงจาก Note เสมอ หากมีระบุไว้ (เช่น "23 พฤศจิกายน 2569, 08:00 - 10:00 (2 ชม.)")
                if (!string.IsNullOrEmpty(b.Note) && b.Note.Contains(","))
                {
                    var noteParts = b.Note.Split(',');
                    var datePart = noteParts[0].Trim();
                    var timePart = noteParts.Length > 1 ? noteParts[1].Trim() : "";
                    var parsedStart = MockDataStore.ParseBookingStartTime(datePart, timePart);
                    var parsedEnd = MockDataStore.ParseBookingEndTime(datePart, timePart);
                    if (parsedStart.HasValue) b.StartTime = parsedStart.Value;
                    if (parsedEnd.HasValue && b.PaymentStatus != "Completed") b.EndTime = parsedEnd.Value;
                }
                else if (b.StartTime.HasValue && !b.EndTime.HasValue)
                {
                    b.EndTime = b.StartTime.Value.AddHours(b.RentalHours > 0 ? b.RentalHours : 2);
                }

                // อัปเดตสถานะให้ถูกต้องตามเงื่อนไขเวลาจริง (เสร็จสิ้น / กำลังใช้งาน / จองล่วงหน้า)
                if (b.MachineId == 0 || (b.Note != null && (b.Note.Contains("Wallet") || b.Note.Contains("เติมเงิน"))))
                {
                    b.PaymentStatus = "Completed";
                }
                else if (b.PaymentStatus == "Completed")
                {
                    // หากผู้ใช้กดคืนเครื่องแล้ว หรือการเช่าเสร็จสิ้นแล้ว ให้คงสถานะ Completed ไว้เสมอ ไม่เปลี่ยนกลับเป็น InUse
                }
                else if (b.PaymentStatus != "Pending" && b.PaymentStatus != "Rejected" && 
                         b.PaymentStatus != "RefundRequested" && b.PaymentStatus != "RefundRejected" && 
                         b.PaymentStatus != "Refunded")
                {
                    if (b.EndTime.HasValue && now >= b.EndTime.Value)
                    {
                        b.PaymentStatus = "Completed"; // สิ้นสุดการเช่าแล้ว
                    }
                    else if (b.StartTime.HasValue && now < b.StartTime.Value)
                    {
                        b.PaymentStatus = "Booked"; // จองล่วงหน้า (ยังไม่ถึงเวลาเริ่มใช้งาน เครื่องยังไม่พร้อมใช้งาน)
                    }
                    else if (b.StartTime.HasValue && now >= b.StartTime.Value && b.EndTime.HasValue && now < b.EndTime.Value)
                    {
                        b.PaymentStatus = "InUse"; // ถึงเวลาเริ่มแล้วและยังไม่หมดเวลา (พร้อมเข้าใช้งานเครื่อง)
                    }
                    else
                    {
                        b.PaymentStatus = "Booked"; // จองล่วงหน้า
                    }
                }
            }

            ViewBag.NewBookings = myBookings;
            ViewBag.ServerNow = now; // ส่งเวลา server ให้ View ใช้คำนวณ countdown

            // คำนวณสถิติจากข้อมูลจริง: นับเฉพาะเครื่องที่กำลังใช้งานอยู่ ณ ปัจจุบันจริง ๆ
            int activeMachines = myBookings.Count(b => b.PaymentStatus == "InUse");
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

        // ==========================================
        // 8. ต่อเวลาใช้งานเครื่องคอมพิวเตอร์ (Extend Booking Realtime API)
        // ==========================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExtendBookingAjax(int bookingId, int hours = 1)
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "กรุณาเข้าสู่ระบบก่อนทำรายการ" });
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            if (hours <= 0) hours = 1;

            // ค้นหารายการจองในระบบ
            var allBookings = await GetAllBookingsAsync();
            var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null || (booking.UserId != userId && userId != 1))
            {
                return Json(new { success = false, message = "ไม่พบรายการจองนี้ หรือคุณไม่มีสิทธิ์จัดการเครื่องนี้" });
            }

            if (booking.PaymentStatus != "Approved" && booking.PaymentStatus != "จอง (พร้อมใช้งาน)" && booking.PaymentStatus != "InUse")
            {
                return Json(new { success = false, message = "ไม่สามารถต่อเวลาได้ เนื่องจากสถานะการเช่าปัจจุบันไม่ได้เปิดใช้งานอยู่" });
            }

            var machine = await GetMachineByIdAsync(booking.MachineId);
            if (machine == null)
            {
                return Json(new { success = false, message = "ไม่พบข้อมูลเครื่องคอมพิวเตอร์นี้ในระบบ" });
            }

            // คำนวณเวลาสิ้นสุดปัจจุบันและเวลาใหม่ที่จะขยายไป
            DateTime currentEndTime = booking.EndTime ?? (booking.StartTime?.AddHours(booking.RentalHours) ?? booking.PaymentDate.AddHours(booking.RentalHours));
            DateTime newEndTime = currentEndTime.AddHours(hours);
            string dateStr = MockDataStore.ExtractDateFromNote(booking.Note) ?? MockDataStore.GetTodayThaiDate();
            string nextSlotStr = $"{currentEndTime:HH:mm} - {newEndTime:HH:mm}";

            // 1. ตรวจสอบเงื่อนไขคิวชน (Slot Conflict Prevention)
            // เช็คว่ามีลูกค้ารายอื่นจองเครื่องนี้ในเวลาที่ต่อเนื่องจากเราหรือไม่
            var conflictingBooking = allBookings.FirstOrDefault(other =>
                other.BookingId != booking.BookingId &&
                other.MachineId == booking.MachineId &&
                (other.PaymentStatus == "Approved" || other.PaymentStatus == "InUse" || other.PaymentStatus == "จอง (พร้อมใช้งาน)" || other.PaymentStatus == "Pending") &&
                other.StartTime.HasValue && other.EndTime.HasValue &&
                other.StartTime.Value < newEndTime && other.EndTime.Value > currentEndTime);

            bool hasSlotConflict = false;
            string conflictTimeDisplay = nextSlotStr + " น.";

            if (conflictingBooking != null)
            {
                hasSlotConflict = true;
                if (conflictingBooking.StartTime.HasValue && conflictingBooking.EndTime.HasValue)
                {
                    conflictTimeDisplay = $"{conflictingBooking.StartTime.Value:HH:mm} - {conflictingBooking.EndTime.Value:HH:mm} น.";
                }
            }
            else
            {
                // ตรวจสอบในระบบลงทะเบียนสล็อตจองล่วงหน้า (_bookedSlotsRegistry)
                var registry = MockDataStore.GetBookedSlotsRegistry();
                for (int h = 0; h < hours; h++)
                {
                    var chkStart = currentEndTime.AddHours(h);
                    var chkEnd = chkStart.AddHours(1);
                    string checkSlot = $"{chkStart:HH:mm} - {chkEnd:HH:mm}";

                    var slotMatch = registry.FirstOrDefault(s =>
                        s.MachineId == booking.MachineId &&
                        MockDataStore.IsSameDate(s.Date, dateStr) &&
                        s.Slot.Equals(checkSlot, StringComparison.OrdinalIgnoreCase) &&
                        s.BookingId != booking.BookingId);

                    if (slotMatch != null)
                    {
                        hasSlotConflict = true;
                        conflictTimeDisplay = checkSlot + " น.";
                        break;
                    }
                }
            }

            if (hasSlotConflict)
            {
                return Json(new
                {
                    success = false,
                    hasConflict = true,
                    conflictTime = conflictTimeDisplay,
                    message = $"⚠️ ไม่สามารถต่อเวลาได้: เนื่องจากมีผู้ใช้งานท่านอื่นจองเครื่องนี้ไว้แล้วในคิวถัดไป ({conflictTimeDisplay}) ระบบไม่อนุญาตให้ต่อเวลาทับซ้อนกับคิวของลูกค้าท่านอื่นครับ"
                });
            }

            // 2. ตรวจสอบยอดเงินในกระเป๋าเงิน (Wallet Balance Check)
            decimal extendCost = machine.PricePerHour * hours;
            decimal currentBal = MockDataStore.GetUserBalance(userId);
            if (currentBal < extendCost)
            {
                return Json(new
                {
                    success = false,
                    hasConflict = false,
                    message = $"ยอดเงินในกระเป๋าเงินไม่เพียงพอสำหรับการต่อเวลา (มี {currentBal:N2} THB ต้องการ {extendCost:N2} THB) กรุณาเติมเงินเข้ากระเป๋าเงินก่อนครับ"
                });
            }

            // 3. หักเงินค่าต่อเวลาจริงจากกระเป๋าเงิน
            MockDataStore.DeductBalance(userId, extendCost, $"ต่อเวลาใช้งานเครื่อง {machine.MachineName} (#CPC-{booking.BookingId}) +{hours} ชม.", $"#EXT-{booking.BookingId}-{DateTime.Now:HHmm}");
            decimal newBal = MockDataStore.GetUserBalance(userId);
            HttpContext.Session.SetString("WalletBalance", newBal.ToString("F2"));

            // 4. บันทึกขยายเวลาใน MockDataStore
            MockDataStore.ExtendBookingTime(booking.BookingId, hours, newEndTime, extendCost);

            // 5. บันทึกขยายเวลาในฐานข้อมูล MySQL (ถ้ามี)
            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbB = await _context.Bookings.FirstOrDefaultAsync(b => b.BookingId == booking.BookingId);
                    if (dbB != null)
                    {
                        dbB.RentalHours += hours;
                        dbB.EndTime = newEndTime;
                        dbB.TotalAmount += extendCost;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            // 6. บันทึกคะแนนสะสมที่ได้รับจากการต่อเวลา
            MockDataStore.AwardBookingPoints(userId, extendCost, machine.MachineName, hours, booking.BookingId);

            int remainingSecs = Math.Max(0, (int)(newEndTime - DateTime.Now).TotalSeconds);

            return Json(new
            {
                success = true,
                newHours = booking.RentalHours + hours,
                newEndTime = newEndTime.ToString("yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                newEndTimeDisplay = newEndTime.ToString("HH:mm น."),
                newTotalAmount = booking.TotalAmount + extendCost,
                remainingSeconds = remainingSecs,
                newBalance = newBal,
                formatted = newBal.ToString("N2") + " THB",
                message = $"หักค่าต่อเวลา -{extendCost:N2} THB สำเร็จ! ขยายเวลาถึง {newEndTime:HH:mm} น. (ยอดคงเหลือ: {newBal:N2} THB)"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnMachineAjax(int bookingId)
        {
            if (!IsLoggedIn())
            {
                return Json(new { success = false, message = "กรุณาเข้าสู่ระบบก่อนทำรายการ" });
            }

            int userId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var allBookings = await GetAllBookingsAsync();
            var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking == null || (booking.UserId != userId && userId != 1))
            {
                return Json(new { success = false, message = "ไม่พบรายการจองนี้" });
            }

            MockDataStore.CompleteBooking(bookingId);

            try
            {
                if (await _context.Database.CanConnectAsync())
                {
                    var dbB = await _context.Bookings.Include(b => b.Machine).FirstOrDefaultAsync(b => b.BookingId == bookingId);
                    if (dbB != null)
                    {
                        dbB.PaymentStatus = "Completed";
                        dbB.EndTime = DateTime.Now;
                        if (dbB.Machine != null && dbB.Machine.Status == "Rented")
                        {
                            dbB.Machine.Status = "Available";
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch { }

            return Json(new { success = true, message = $"คืนเครื่อง #CPC-{bookingId} เรียบร้อยแล้ว ขอบคุณที่เลือกใช้บริการ CloudPC ครับ" });
        }
    }
}
