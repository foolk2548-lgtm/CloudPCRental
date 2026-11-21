using System.Collections.Concurrent;

namespace CloudPCRental.Models
{
    public class WalletTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Title { get; set; } = string.Empty;
        public string ReferenceCode { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public decimal Amount { get; set; } // ค่าบวก = เติม/คืนเงิน, ค่าลบ = หักชำระ
        public string Status { get; set; } = "สำเร็จ";
    }

    public class BookedSlotInfo
    {
        public int MachineId { get; set; }
        public string Date { get; set; } = string.Empty; // เช่น "10 ตุลาคม 2566"
        public string Slot { get; set; } = string.Empty; // เช่น "08:00 - 09:00"
        public int BookingId { get; set; }
        public int UserId { get; set; }
    }

    public class MockDataStoreSnapshot
    {
        public List<Machine> Machines { get; set; } = new();
        public List<User> Users { get; set; } = new();
        public List<BookingSnapshotDto> Bookings { get; set; } = new();
        public Dictionary<int, decimal> UserBalances { get; set; } = new();
        public List<WalletTransaction> WalletTransactions { get; set; } = new();
        public List<BookedSlotInfo> BookedSlotsRegistry { get; set; } = new();
        public List<SupportTicket> SupportTickets { get; set; } = new();
        public List<SystemLog> SystemLogs { get; set; } = new();
        public Dictionary<int, int> UserPoints { get; set; } = new();
        public Dictionary<int, string> UserPhoneNumbers { get; set; } = new();
        public List<PointTransaction> PointTransactions { get; set; } = new();
        public int NextBookingId { get; set; } = 95000;
        public int NextTxnId { get; set; } = 200;
        public int NextPointTxnId { get; set; } = 500;
        public int NextTicketId { get; set; } = 1003;
        public int NextMessageId { get; set; } = 5010;
        public int NextLogId { get; set; } = 100;
    }

    public class BookingSnapshotDto
    {
        public int BookingId { get; set; }
        public int UserId { get; set; }
        public int MachineId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public string? SlipImageUrl { get; set; }
        public string? RefundSlipImageUrl { get; set; }
        public string? RefundReason { get; set; }
        public DateTime? RefundDate { get; set; }
        public string? Note { get; set; }
        public int RentalHours { get; set; } = 1;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public static class MockDataStore
    {
        private static readonly List<Machine> _machines = new();
        private static readonly List<User> _users = new();
        private static readonly List<Booking> _bookings = new();
        private static readonly ConcurrentDictionary<int, decimal> _userBalances = new();
        private static readonly List<WalletTransaction> _walletTransactions = new();
        private static readonly List<BookedSlotInfo> _bookedSlotsRegistry = new();
        private static readonly List<SupportTicket> _supportTickets = new();
        private static readonly List<SystemLog> _systemLogs = new();
        private static readonly ConcurrentDictionary<int, int> _userPoints = new();
        private static readonly ConcurrentDictionary<int, string> _userPhoneNumbers = new();
        private static readonly List<PointTransaction> _pointTransactions = new();
        private static int _nextTxnId = 200;
        private static int _nextPointTxnId = 500;
        private static int _nextTicketId = 1003;
        private static int _nextMessageId = 5010;
        private static int _nextLogId = 100;
        private static readonly object _lock = new();

        public static string GetDataFilePath()
        {
            string appDataDir = Path.Combine(Directory.GetCurrentDirectory(), "App_Data");
            if (!Directory.Exists(appDataDir))
            {
                Directory.CreateDirectory(appDataDir);
            }
            return Path.Combine(appDataDir, "mock_datastore.json");
        }

        public static void SaveToFile()
        {
            lock (_lock)
            {
                try
                {
                    var snapshot = new MockDataStoreSnapshot
                    {
                        Machines = _machines.Select(m => new Machine
                        {
                            MachineId = m.MachineId,
                            MachineName = m.MachineName,
                            CPU = m.CPU,
                            RAM = m.RAM,
                            GPU = m.GPU,
                            PricePerHour = m.PricePerHour,
                            Status = m.Status,
                            RepairNote = m.RepairNote,
                            ReportedBy = m.ReportedBy,
                            ReportedAt = m.ReportedAt,
                            ApprovedBy = m.ApprovedBy
                        }).ToList(),
                        Users = _users.Select(u => new User
                        {
                            UserId = u.UserId,
                            Username = u.Username,
                            Email = u.Email,
                            Password = u.Password,
                            Role = u.Role
                        }).ToList(),
                        Bookings = _bookings.Select(b => new BookingSnapshotDto
                        {
                            BookingId = b.BookingId,
                            UserId = b.UserId,
                            MachineId = b.MachineId,
                            TotalAmount = b.TotalAmount,
                            PaymentDate = b.PaymentDate,
                            PaymentStatus = b.PaymentStatus,
                            SlipImageUrl = b.SlipImageUrl,
                            RefundSlipImageUrl = b.RefundSlipImageUrl,
                            RefundReason = b.RefundReason,
                            RefundDate = b.RefundDate,
                            Note = b.Note,
                            RentalHours = b.RentalHours,
                            StartTime = b.StartTime,
                            EndTime = b.EndTime
                        }).ToList(),
                        UserBalances = _userBalances.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        WalletTransactions = _walletTransactions.ToList(),
                        BookedSlotsRegistry = _bookedSlotsRegistry.ToList(),
                        SupportTickets = _supportTickets.ToList(),
                        SystemLogs = _systemLogs.ToList(),
                        UserPoints = _userPoints.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        PointTransactions = _pointTransactions.ToList(),
                        UserPhoneNumbers = _userPhoneNumbers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                        NextBookingId = _nextBookingId,
                        NextTxnId = _nextTxnId,
                        NextPointTxnId = _nextPointTxnId,
                        NextTicketId = _nextTicketId,
                        NextMessageId = _nextMessageId,
                        NextLogId = _nextLogId
                    };

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                    };

                    string json = System.Text.Json.JsonSerializer.Serialize(snapshot, options);
                    string path = GetDataFilePath();
                    File.WriteAllText(path, json, System.Text.Encoding.UTF8);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MockDataStore] Failed to save state to file: {ex.Message}");
                }
            }
        }

        public static bool LoadFromFile()
        {
            lock (_lock)
            {
                try
                {
                    string path = GetDataFilePath();
                    if (!File.Exists(path)) return false;

                    string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                    if (string.IsNullOrWhiteSpace(json)) return false;

                    var snapshot = System.Text.Json.JsonSerializer.Deserialize<MockDataStoreSnapshot>(json);
                    if (snapshot == null || snapshot.Machines == null || !snapshot.Machines.Any()) return false;

                    _machines.Clear();
                    _machines.AddRange(snapshot.Machines);

                    _users.Clear();
                    _users.AddRange(snapshot.Users);

                    _userBalances.Clear();
                    if (snapshot.UserBalances != null)
                    {
                        foreach (var kvp in snapshot.UserBalances)
                        {
                            _userBalances[kvp.Key] = kvp.Value;
                        }
                    }

                    _walletTransactions.Clear();
                    if (snapshot.WalletTransactions != null)
                    {
                        _walletTransactions.AddRange(snapshot.WalletTransactions);
                    }

                    _bookedSlotsRegistry.Clear();
                    if (snapshot.BookedSlotsRegistry != null)
                    {
                        _bookedSlotsRegistry.AddRange(snapshot.BookedSlotsRegistry);
                    }

                    _nextBookingId = snapshot.NextBookingId;
                    _nextTxnId = snapshot.NextTxnId;

                    _bookings.Clear();
                    if (snapshot.Bookings != null)
                    {
                        foreach (var dto in snapshot.Bookings)
                        {
                            _bookings.Add(new Booking
                            {
                                BookingId = dto.BookingId,
                                UserId = dto.UserId,
                                MachineId = dto.MachineId,
                                TotalAmount = dto.TotalAmount,
                                PaymentDate = dto.PaymentDate,
                                PaymentStatus = dto.PaymentStatus,
                                SlipImageUrl = dto.SlipImageUrl,
                                RefundSlipImageUrl = dto.RefundSlipImageUrl,
                                RefundReason = dto.RefundReason,
                                RefundDate = dto.RefundDate,
                                Note = dto.Note,
                                RentalHours = dto.RentalHours,
                                StartTime = dto.StartTime,
                                EndTime = dto.EndTime,
                                User = _users.FirstOrDefault(u => u.UserId == dto.UserId),
                                Machine = _machines.FirstOrDefault(m => m.MachineId == dto.MachineId)
                            });
                        }
                    }

                    _supportTickets.Clear();
                    if (snapshot.SupportTickets != null && snapshot.SupportTickets.Any())
                    {
                        _supportTickets.AddRange(snapshot.SupportTickets);
                    }
                    else
                    {
                        EnsureDefaultTickets();
                    }
                    _nextTicketId = snapshot.NextTicketId > 1000 ? snapshot.NextTicketId : 1003;
                    _nextMessageId = snapshot.NextMessageId > 5000 ? snapshot.NextMessageId : 5010;

                    _userPoints.Clear();
                    if (snapshot.UserPoints != null)
                    {
                        foreach (var kvp in snapshot.UserPoints)
                        {
                            _userPoints[kvp.Key] = kvp.Value;
                        }
                    }

                    _pointTransactions.Clear();
                    if (snapshot.PointTransactions != null)
                    {
                        _pointTransactions.AddRange(snapshot.PointTransactions);
                    }
                    _nextPointTxnId = snapshot.NextPointTxnId > 500 ? snapshot.NextPointTxnId : 500;

                    _userPhoneNumbers.Clear();
                    if (snapshot.UserPhoneNumbers != null)
                    {
                        foreach (var kvp in snapshot.UserPhoneNumbers)
                        {
                            _userPhoneNumbers[kvp.Key] = kvp.Value;
                        }
                    }

                    EnsureUserPointsAndHistory();

                    _systemLogs.Clear();
                    if (snapshot.SystemLogs != null && snapshot.SystemLogs.Any())
                    {
                        _systemLogs.AddRange(snapshot.SystemLogs);
                    }
                    else
                    {
                        EnsureDefaultLogs();
                    }
                    _nextLogId = snapshot.NextLogId > 100 ? snapshot.NextLogId : 100;

                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MockDataStore] Failed to load state from file: {ex.Message}");
                    return false;
                }
            }
        }

        public static string GetTodayThaiDate()
        {
            return DateTime.Now.ToString("d MMMM yyyy", new System.Globalization.CultureInfo("th-TH"));
        }

        public static string GetCurrentThaiDateTime()
        {
            return DateTime.Now.ToString("d MMM yyyy, HH:mm:ss น.", new System.Globalization.CultureInfo("th-TH"));
        }

        static MockDataStore()
        {
            if (!LoadFromFile())
            {
                ResetToDefault();
                SaveToFile();
            }
        }

        public static void ResetToDefault(bool deleteFile = false)
        {
            lock (_lock)
            {
                if (deleteFile)
                {
                    try
                    {
                        string path = GetDataFilePath();
                        if (File.Exists(path)) File.Delete(path);
                    }
                    catch { }
                }

                _machines.Clear();
                _users.Clear();
                _bookings.Clear();
                _walletTransactions.Clear();
                _bookedSlotsRegistry.Clear();
                _userPoints.Clear();
                _pointTransactions.Clear();
                _nextBookingId = 95000;
                _nextTxnId = 200;
                _nextPointTxnId = 500;

                // ค่าเริ่มต้นจำลอง: เครื่อง 2 มีคิวจองช่วง 11:00 - 13:00 สำหรับวันที่ 10 ตุลาคม 2566 และวันที่ปัจจุบัน
                _bookedSlotsRegistry.Add(new BookedSlotInfo { MachineId = 2, Date = "10 ตุลาคม 2566", Slot = "11:00 - 12:00", BookingId = 1001, UserId = 6 });
                _bookedSlotsRegistry.Add(new BookedSlotInfo { MachineId = 2, Date = "10 ตุลาคม 2566", Slot = "12:00 - 13:00", BookingId = 1001, UserId = 6 });
                _bookedSlotsRegistry.Add(new BookedSlotInfo { MachineId = 2, Date = GetTodayThaiDate(), Slot = "11:00 - 12:00", BookingId = 1001, UserId = 6 });
                _bookedSlotsRegistry.Add(new BookedSlotInfo { MachineId = 2, Date = GetTodayThaiDate(), Slot = "12:00 - 13:00", BookingId = 1001, UserId = 6 });

                // ตั้งค่ายอดเงินกระเป๋าเงินเริ่มต้น: thanapol ได้รับเงินคืน 60 บาทจากฝ่ายบัญชี รวมเป็น 600 บาท
                _userBalances[5] = 600.00m; // thanapol (540 + 60 THB คืนเงินสำเร็จ)
                _userBalances[6] = 540.00m; // user1
                _userBalances[8] = 540.00m; // somjet

                _walletTransactions.AddRange(new List<WalletTransaction>
                {
                    new WalletTransaction { Id = 1, UserId = 5, CreatedAt = DateTime.Now.AddMinutes(-5), Title = "ฝ่ายบัญชีอนุมัติคืนเงินคำขอ #CPC-94812 (Extreme Gaming PC - RTX 4090)", ReferenceCode = "#REFUND-94812", Channel = "คืนเงินเข้ากระเป๋าเงิน CloudPC Wallet", Amount = 60.00m, Status = "สำเร็จ" },
                    new WalletTransaction { Id = 2, UserId = 5, CreatedAt = DateTime.Now.AddDays(-2), Title = "ชำระค่าเช่า Extreme Gaming PC - RTX 4090", ReferenceCode = "#CPC-94812 (4 ชม.)", Channel = "กระเป๋าเงิน CloudPC", Amount = -60.00m, Status = "สำเร็จ" },
                    new WalletTransaction { Id = 3, UserId = 5, CreatedAt = DateTime.Now.AddDays(-2).AddHours(-3), Title = "เติมเงินเข้ากระเป๋าเงิน", ReferenceCode = "#TOPUP-20231025-01", Channel = "พร้อมเพย์ QR Code", Amount = 300.00m, Status = "สำเร็จ" },
                    new WalletTransaction { Id = 4, UserId = 5, CreatedAt = DateTime.Now.AddDays(-3), Title = "ชำระค่าเช่า Standard Office PC - Core i5", ReferenceCode = "#CPC-93721 (3 ชม.)", Channel = "กระเป๋าเงิน CloudPC", Amount = -18.00m, Status = "สำเร็จ" },
                    new WalletTransaction { Id = 5, UserId = 5, CreatedAt = DateTime.Now.AddDays(-7), Title = "เติมเงินเข้ากระเป๋าเงิน", ReferenceCode = "#TOPUP-20231020-04", Channel = "บัตรเครดิต Visa", Amount = 500.00m, Status = "สำเร็จ" },
                    new WalletTransaction { Id = 6, UserId = 5, CreatedAt = DateTime.Now.AddDays(-15), Title = "โบนัสต้อนรับสมาชิกใหม่", ReferenceCode = "กิจกรรมเปิดระบบ CloudPC Thailand", Channel = "สิทธิพิเศษระบบ", Amount = 50.00m, Status = "สำเร็จ" }
                });

                // 1. เครื่องจำลอง 22 เครื่องตรงตาม Wireframe หน้า 3
                // (ว่าง 12 เครื่อง, กำลังใช้งาน 8 เครื่อง, ซ่อมบำรุง 2 เครื่อง)
                _machines.AddRange(new List<Machine>
                {
                    // 1. Gaming - รอผู้จัดการอนุมัติซ่อม (แจ้งโดยพนักงาน)
                    new Machine { 
                        MachineId = 1, 
                        MachineName = "เครื่องเล่นเกมสเปกแรง PRO 01", 
                        CPU = "AMD Ryzen 9 7900X", 
                        RAM = "32GB DDR5", 
                        GPU = "RTX 4080 (16GB)", 
                        PricePerHour = 15.00m, 
                        Status = "PendingRepair",
                        RepairNote = "จอฟ้าบ่อยครั้ง การ์ดจอตัดการทำงานเมื่อเล่นเกมกราฟิกสูง",
                        ReportedBy = "กานดา (พนักงานหน้าร้าน)",
                        ReportedAt = DateTime.Now.AddHours(-1)
                    },
                    
                    // 2. Graphic - กำลังใช้งาน (1)
                    new Machine { MachineId = 2, MachineName = "เครื่องทำงานตัดต่อ Render 02", CPU = "Intel Core i9-13900K", RAM = "64GB DDR5", GPU = "RTX 4090 (24GB)", PricePerHour = 25.00m, Status = "Rented" },
                    
                    // 3. AI - ว่าง (2)
                    new Machine { MachineId = 3, MachineName = "เครื่องประมวลผล AI/Data 03", CPU = "AMD EPYC 9354", RAM = "128GB DDR5", GPU = "A100 GPU (80GB)", PricePerHour = 40.00m, Status = "Available" },
                    
                    // 4. Office - ว่าง (3)
                    new Machine { MachineId = 4, MachineName = "เครื่องสเปกเริ่มต้นทั่วไป Work 04", CPU = "Intel Core i5-13400", RAM = "16GB DDR4", GPU = "RTX 3060 (12GB)", PricePerHour = 6.00m, Status = "Available" },
                    
                    // 5. Graphic - ซ่อมบำรุง (อนุมัติแล้ว แจ้งช่างแล้ว)
                    new Machine { 
                        MachineId = 5, 
                        MachineName = "เครื่องดีไซเนอร์สเปกทำงาน 05", 
                        CPU = "Intel Core i7-13700", 
                        RAM = "32GB DDR5", 
                        GPU = "RTX 4070 (12GB)", 
                        PricePerHour = 12.00m, 
                        Status = "Maintenance",
                        RepairNote = "พัดลมเคสและไฟเลี้ยงการ์ดจอมีเสียงดัง ช่างนัดเข้าซ่อมบ่ายนี้",
                        ReportedBy = "กานดา (พนักงานหน้าร้าน)",
                        ApprovedBy = "วีระ (ผู้จัดการ)",
                        ReportedAt = DateTime.Now.AddHours(-4)
                    },
                    
                    // 6. AI - ว่าง (4)
                    new Machine { MachineId = 6, MachineName = "เซิร์ฟเวอร์จำลองโปรแกรม Dev 06", CPU = "Intel Xeon W-2245", RAM = "64GB DDR4 ECC", GPU = "RTX 3080 (10GB)", PricePerHour = 18.00m, Status = "Available" },
                    
                    // 7. Gaming - ว่าง (5)
                    new Machine { MachineId = 7, MachineName = "เครื่องเกมมิ่งสปีดสูง Speed 07", CPU = "AMD Ryzen 7 7800X3D", RAM = "32GB DDR5", GPU = "RTX 4080 Super", PricePerHour = 20.00m, Status = "Available" },
                    
                    // 8. Graphic - กำลังใช้งาน (2)
                    new Machine { MachineId = 8, MachineName = "เวิร์กสเตชันตัดต่อ 4K Studio 08", CPU = "Intel Core i9-14900K", RAM = "64GB DDR5", GPU = "RTX 4080 (16GB)", PricePerHour = 22.00m, Status = "Rented" },
                    
                    // 9. AI - ว่าง (6)
                    new Machine { MachineId = 9, MachineName = "ดีพเลิร์นนิงซูเปอร์โหนด AI 09", CPU = "AMD Threadripper 7960X", RAM = "128GB DDR5", GPU = "RTX 4090 x2", PricePerHour = 50.00m, Status = "Available" },
                    
                    // 10. Office - ว่าง (7)
                    new Machine { MachineId = 10, MachineName = "เครื่องสำนักงานพิมพ์งานเบา Office 10", CPU = "Intel Core i3-13100", RAM = "16GB DDR4", GPU = "GTX 1650 (4GB)", PricePerHour = 5.00m, Status = "Available" },
                    
                    // 11. Gaming - ว่าง (8)
                    new Machine { MachineId = 11, MachineName = "แคสเตอร์เกมออนไลน์ Stream 11", CPU = "AMD Ryzen 7 7700X", RAM = "32GB DDR5", GPU = "RTX 4070 Ti", PricePerHour = 16.00m, Status = "Available" },
                    
                    // 12. Graphic - กำลังใช้งาน (3)
                    new Machine { MachineId = 12, MachineName = "เครื่องเรนเดอร์ 3D Max 12", CPU = "Intel Core i9-13900KF", RAM = "64GB DDR5", GPU = "RTX 4090", PricePerHour = 28.00m, Status = "Rented" },
                    
                    // 13. AI - กำลังใช้งาน (4)
                    new Machine { MachineId = 13, MachineName = "ดาต้าไซเอนซ์โหนด BigData 13", CPU = "AMD EPYC 7763", RAM = "256GB DDR4", GPU = "NVIDIA A100", PricePerHour = 45.00m, Status = "Rented" },
                    
                    // 14. Office - ว่าง (9)
                    new Machine { MachineId = 14, MachineName = "มัลติมีเดียทั่วไป Work 14", CPU = "Intel Core i5-12400", RAM = "16GB DDR4", GPU = "RTX 3050", PricePerHour = 7.00m, Status = "Available" },
                    
                    // 15. Gaming - ว่าง (10)
                    new Machine { MachineId = 15, MachineName = "ซูเปอร์เกมมิ่งเรด Gaming 15", CPU = "AMD Ryzen 9 7950X3D", RAM = "64GB DDR5", GPU = "RTX 4090", PricePerHour = 30.00m, Status = "Available" },
                    
                    // 16. Graphic - กำลังใช้งาน (5)
                    new Machine { MachineId = 16, MachineName = "สตูดิโอโมชันกราฟิก Studio 16", CPU = "Intel Core i9-14900KS", RAM = "64GB DDR5", GPU = "RTX 4090 (24GB)", PricePerHour = 32.00m, Status = "Rented" },
                    
                    // 17. AI - ซ่อมบำรุง (2)
                    new Machine { MachineId = 17, MachineName = "แมชชีนเลิร์นนิงแล็บ Cluster 17", CPU = "Intel Xeon Platinum", RAM = "128GB DDR5", GPU = "RTX 6000 Ada", PricePerHour = 65.00m, Status = "Maintenance" },
                    
                    // 18. Office - ว่าง (11)
                    new Machine { MachineId = 18, MachineName = "งานเอกสารและบัญชี Office 18", CPU = "Intel Core i5-11400", RAM = "16GB DDR4", GPU = "GTX 1050 Ti", PricePerHour = 5.00m, Status = "Available" },
                    
                    // 19. Gaming - ว่าง (12)
                    new Machine { MachineId = 19, MachineName = "โปรเพลเยอร์อีสปอร์ต E-Sport 19", CPU = "Intel Core i7-14700K", RAM = "32GB DDR5", GPU = "RTX 4070 Super", PricePerHour = 18.00m, Status = "Available" },
                    
                    // 20. Graphic - กำลังใช้งาน (6)
                    new Machine { MachineId = 20, MachineName = "วิดีโอโปรดักชันเรนเดอร์ Cut 20", CPU = "AMD Ryzen 9 7900", RAM = "64GB DDR5", GPU = "RTX 4080", PricePerHour = 24.00m, Status = "Rented" },
                    
                    // 21. AI - กำลังใช้งาน (7)
                    new Machine { MachineId = 21, MachineName = "ปัญญาประดิษฐ์เทรนนิ่ง Train 21", CPU = "Dual AMD EPYC", RAM = "256GB DDR5", GPU = "H100 (80GB)", PricePerHour = 80.00m, Status = "Rented" },
                    
                    // 22. Office - กำลังใช้งาน (8)
                    new Machine { MachineId = 22, MachineName = "สำนักงานสาขาทั่วไป Office 22", CPU = "AMD Ryzen 5 5600G", RAM = "16GB DDR4", GPU = "Radeon Vega", PricePerHour = 6.00m, Status = "Rented" }
                });

                // 2. ผู้ใช้งานเริ่มต้นสำหรับทดสอบระบบ (ครบทุก Role ตามโครงสร้างองค์กรใน Wireframe)
                _users.AddRange(new List<User>
                {
                    new User { UserId = 1, Username = "admin", Email = "admin@cloudpc.com", Password = "admin123", Role = "Admin" },
                    new User { UserId = 2, Username = "manager", Email = "manager@cloudpc.com", Password = "manager123", Role = "Manager" },
                    new User { UserId = 3, Username = "staff", Email = "staff@cloudpc.com", Password = "staff123", Role = "Staff" },
                    new User { UserId = 4, Username = "accountant", Email = "account@cloudpc.com", Password = "account123", Role = "Accountant" },
                    new User { UserId = 5, Username = "thanapol", Email = "thanapol@example.com", Password = "user123", Role = "User" },
                    new User { UserId = 6, Username = "user1", Email = "user1@example.com", Password = "user123", Role = "User" },
                    new User { UserId = 7, Username = "somchai", Email = "somchai@cloudpc.com", Password = "password123", Role = "Staff" },
                    new User { UserId = 8, Username = "somjet", Email = "somjet.game@gmail.com", Password = "password123", Role = "User" }
                });

                SaveToFile();
            }
        }

        public static List<Machine> GetMachines()
        {
            lock (_lock)
            {
                return _machines.Select(m => new Machine
                {
                    MachineId = m.MachineId,
                    MachineName = m.MachineName,
                    CPU = m.CPU,
                    RAM = m.RAM,
                    GPU = m.GPU,
                    PricePerHour = m.PricePerHour,
                    Status = m.Status,
                    RepairNote = m.RepairNote,
                    ReportedBy = m.ReportedBy,
                    ReportedAt = m.ReportedAt,
                    ApprovedBy = m.ApprovedBy
                }).ToList();
            }
        }

        public static Machine? GetMachineById(int id)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == id);
                if (m == null) return null;
                return new Machine
                {
                    MachineId = m.MachineId,
                    MachineName = m.MachineName,
                    CPU = m.CPU,
                    RAM = m.RAM,
                    GPU = m.GPU,
                    PricePerHour = m.PricePerHour,
                    Status = m.Status,
                    RepairNote = m.RepairNote,
                    ReportedBy = m.ReportedBy,
                    ReportedAt = m.ReportedAt,
                    ApprovedBy = m.ApprovedBy
                };
            }
        }

        public static void RequestMachineRepair(int id, string issueType, string note, string reportedBy)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == id);
                if (m != null)
                {
                    m.Status = "PendingRepair";
                    m.RepairNote = string.IsNullOrWhiteSpace(issueType) ? note : $"[{issueType}] {note}";
                    m.ReportedBy = reportedBy;
                    m.ReportedAt = DateTime.Now;
                    m.ApprovedBy = null;
                }
                else
                {
                    _machines.Add(new Machine
                    {
                        MachineId = id,
                        MachineName = $"เครื่องคอมพิวเตอร์ PC-{id:D3}",
                        Status = "PendingRepair",
                        RepairNote = string.IsNullOrWhiteSpace(issueType) ? note : $"[{issueType}] {note}",
                        ReportedBy = reportedBy,
                        ReportedAt = DateTime.Now
                    });
                }
                SaveToFile();
            }
        }

        public static void ApproveMachineRepair(int id, string approvedBy)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == id);
                if (m != null)
                {
                    m.Status = "Maintenance";
                    m.ApprovedBy = approvedBy;
                    SaveToFile();
                }
            }
        }

        public static void RejectMachineRepair(int id)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == id);
                if (m != null)
                {
                    m.Status = "Available";
                    m.RepairNote = null;
                    m.ReportedBy = null;
                    m.ReportedAt = null;
                    m.ApprovedBy = null;
                    SaveToFile();
                }
            }
        }

        public static void FinishMachineRepair(int id)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == id);
                if (m != null)
                {
                    m.Status = "Available";
                    m.RepairNote = null;
                    m.ReportedBy = null;
                    m.ReportedAt = null;
                    m.ApprovedBy = null;
                    SaveToFile();
                }
            }
        }

        public static void UpdateMachineStatus(int id, string newStatus)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == id);
                if (m != null)
                {
                    // หากเครื่องกำลังรออนุมัติซ่อม หรือกำลังซ่อมบำรุง ไม่อนุญาตให้เปลี่ยนเป็น Rented หรือ Available อัตโนมัติ
                    if ((m.Status == "PendingRepair" || m.Status == "Maintenance") && (newStatus == "Rented" || newStatus == "Available"))
                    {
                        return;
                    }
                    m.Status = newStatus;
                    SaveToFile();
                }
            }
        }

        // แจ้งซ่อมด่วนและระงับเครื่องกำลังใช้งาน (ฉุกเฉิน)
        public static (bool success, string message, decimal refundAmount, int? refundedUserId) EmergencyReportRepair(
            int machineId, 
            string issueType, 
            string note, 
            bool refundCustomer, 
            string reportedBy)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == machineId);
                if (m == null) return (false, "ไม่พบข้อมูลเครื่องในระบบ", 0, null);

                m.Status = "PendingRepair";
                m.RepairNote = string.IsNullOrWhiteSpace(issueType) ? note : $"[แจ้งซ่อมด่วนระหว่างใช้งาน: {issueType}] {note}";
                m.ReportedBy = reportedBy;
                m.ReportedAt = DateTime.Now;
                m.ApprovedBy = null;

                decimal refundAmount = 0m;
                int? refundedUserId = null;

                if (refundCustomer)
                {
                    var b = _bookings.FirstOrDefault(x => x.MachineId == machineId && (x.PaymentStatus == "Approved" || x.PaymentStatus == "จอง (พร้อมใช้งาน)"));
                    if (b != null)
                    {
                        refundAmount = b.TotalAmount > 0 ? b.TotalAmount : 60.00m;
                        refundedUserId = b.UserId;
                        b.PaymentStatus = "Refunded";
                        b.RefundDate = DateTime.Now;
                        b.Note = $"ระงับการเช่าฉุกเฉินและคืนเงิน (+{refundAmount:N2} ฿) เนื่องจากเครื่องมีปัญหา: {note}";

                        RefundBalance(b.UserId, refundAmount, $"ฝ่ายดูแลระบบคืนเงินค่าเช่าฉุกเฉิน PC-{machineId:D3} ({note})", $"#EMERG-{machineId:D3}");
                    }
                    else
                    {
                        refundAmount = 60.00m;
                        refundedUserId = 5;
                        RefundBalance(5, refundAmount, $"ฝ่ายดูแลระบบคืนเงินค่าเช่าฉุกเฉิน PC-{machineId:D3} ({note})", $"#EMERG-{machineId:D3}");
                    }
                }

                SaveToFile();
                return (true, $"แจ้งซ่อมด่วนและระงับเครื่อง PC-{machineId:D3} เรียบร้อยแล้ว (สถานะ: รอผู้จัดการอนุมัติ)" + (refundCustomer ? $" และคืนเงิน {refundAmount:N2} บาทเข้ากระเป๋าเงินลูกค้าเรียบร้อยแล้ว" : ""), refundAmount, refundedUserId);
            }
        }

        // ปลดล็อก / สิ้นสุดการเช่าฉุกเฉิน
        public static (bool success, string message, decimal refundAmount, int? refundedUserId) EmergencyReleaseMachine(
            int machineId, 
            string reason, 
            string nextStatus, 
            bool refundCustomer, 
            string operatorName)
        {
            lock (_lock)
            {
                var m = _machines.FirstOrDefault(x => x.MachineId == machineId);
                if (m == null) return (false, "ไม่พบข้อมูลเครื่องในระบบ", 0, null);

                string targetStatus = nextStatus == "PendingRepair" ? "PendingRepair" : "Available";
                m.Status = targetStatus;

                if (targetStatus == "PendingRepair")
                {
                    m.RepairNote = $"[ระงับเครื่อง/ส่งซ่อม] {reason}";
                    m.ReportedBy = operatorName;
                    m.ReportedAt = DateTime.Now;
                    m.ApprovedBy = null;
                }
                else
                {
                    m.RepairNote = null;
                    m.ReportedBy = null;
                    m.ReportedAt = null;
                    m.ApprovedBy = null;
                }

                decimal refundAmount = 0m;
                int? refundedUserId = null;

                if (refundCustomer)
                {
                    var b = _bookings.FirstOrDefault(x => x.MachineId == machineId && (x.PaymentStatus == "Approved" || x.PaymentStatus == "จอง (พร้อมใช้งาน)"));
                    if (b != null)
                    {
                        refundAmount = b.TotalAmount > 0 ? b.TotalAmount : 60.00m;
                        refundedUserId = b.UserId;
                        b.PaymentStatus = "Refunded";
                        b.RefundDate = DateTime.Now;
                        b.Note = $"ฝ่ายดูแลระบบสิ้นสุดการเช่าฉุกเฉินและคืนเงิน (+{refundAmount:N2} ฿) เหตุผล: {reason}";

                        RefundBalance(b.UserId, refundAmount, $"สิ้นสุดการเช่าฉุกเฉินและคืนเงิน PC-{machineId:D3} ({reason})", $"#RELEASE-{machineId:D3}");
                    }
                    else
                    {
                        refundAmount = 60.00m;
                        refundedUserId = 5;
                        RefundBalance(5, refundAmount, $"สิ้นสุดการเช่าฉุกเฉินและคืนเงิน PC-{machineId:D3} ({reason})", $"#RELEASE-{machineId:D3}");
                    }
                }

                SaveToFile();
                return (true, $"ปลดล็อกเครื่อง PC-{machineId:D3} สู่สถานะ '{(targetStatus == "Available" ? "พร้อมใช้งาน" : "รออนุมัติซ่อม")}' เรียบร้อยแล้ว" + (refundCustomer ? $" (คืนเงิน {refundAmount:N2} บาทเข้ากระเป๋าเงินลูกค้าแล้ว)" : ""), refundAmount, refundedUserId);
            }
        }

        public static void AddMachine(Machine machine)
        {
            lock (_lock)
            {
                int newId = _machines.Any() ? _machines.Max(m => m.MachineId) + 1 : 1;
                machine.MachineId = newId;
                _machines.Add(machine);
                SaveToFile();
            }
        }

        public static void UpdateMachine(Machine machine)
        {
            lock (_lock)
            {
                var existing = _machines.FirstOrDefault(m => m.MachineId == machine.MachineId);
                if (existing != null)
                {
                    existing.MachineName = machine.MachineName;
                    existing.CPU = machine.CPU;
                    existing.RAM = machine.RAM;
                    existing.GPU = machine.GPU;
                    existing.PricePerHour = machine.PricePerHour;
                    existing.Status = machine.Status;
                    SaveToFile();
                }
            }
        }

        public static void DeleteMachine(int id)
        {
            lock (_lock)
            {
                _machines.RemoveAll(m => m.MachineId == id);
                SaveToFile();
            }
        }

        public static List<User> GetUsers()
        {
            lock (_lock)
            {
                return _users.Select(u => new User
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    Email = u.Email,
                    Password = u.Password,
                    Role = u.Role
                }).ToList();
            }
        }

        public static User? GetUserById(int id)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => u.UserId == id);
            }
        }

        public static User? Authenticate(string usernameOrEmail, string password)
        {
            lock (_lock)
            {
                return _users.FirstOrDefault(u => 
                    (u.Username.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase) || 
                     u.Email.Equals(usernameOrEmail, StringComparison.OrdinalIgnoreCase)) && 
                    u.Password == password);
            }
        }

        public static void AddUser(User user)
        {
            lock (_lock)
            {
                int newId = _users.Any() ? _users.Max(u => u.UserId) + 1 : 1;
                user.UserId = newId;
                _users.Add(user);
                _userBalances[newId] = 0.00m;
                _userPoints[newId] = 0;
                SaveToFile();
            }
        }

        public static void UpdateUser(User user)
        {
            lock (_lock)
            {
                var existing = _users.FirstOrDefault(u => u.UserId == user.UserId);
                if (existing != null)
                {
                    existing.Username = user.Username;
                    existing.Email = user.Email;
                    existing.Password = user.Password;
                    existing.Role = user.Role;
                    SaveToFile();
                }
            }
        }

        public static string GetUserPhoneNumber(int userId)
        {
            if (_userPhoneNumbers.TryGetValue(userId, out var phone) && !string.IsNullOrWhiteSpace(phone))
            {
                return phone;
            }
            return "081-234-5678";
        }

        public static void SetUserPhoneNumber(int userId, string phone)
        {
            _userPhoneNumbers[userId] = phone;
            SaveToFile();
        }

        public static void DeleteUser(int id)
        {
            lock (_lock)
            {
                _users.RemoveAll(u => u.UserId == id);
                _userBalances.TryRemove(id, out _);
                _userPoints.TryRemove(id, out _);
                _userPhoneNumbers.TryRemove(id, out _);
                SaveToFile();
            }
        }

        // ==========================================
        // Helper Methods สำหรับฝ่ายบัญชี (Bookings)
        // ==========================================
        public static List<Booking> GetBookings()
        {
            lock (_lock)
            {
                return _bookings.Select(b => new Booking
                {
                    BookingId = b.BookingId,
                    UserId = b.UserId,
                    MachineId = b.MachineId,
                    RentalHours = b.RentalHours,
                    TotalAmount = b.TotalAmount,
                    PaymentDate = b.PaymentDate,
                    PaymentStatus = b.PaymentStatus,
                    SlipImageUrl = b.SlipImageUrl,
                    RefundSlipImageUrl = b.RefundSlipImageUrl,
                    RefundReason = b.RefundReason,
                    RefundDate = b.RefundDate,
                    Note = b.Note,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    User = _users.FirstOrDefault(u => u.UserId == b.UserId),
                    Machine = _machines.FirstOrDefault(m => m.MachineId == b.MachineId)
                }).ToList();
            }
        }

        public static Booking? GetBookingById(int id)
        {
            lock (_lock)
            {
                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                if (b == null) return null;
                return new Booking
                {
                    BookingId = b.BookingId,
                    UserId = b.UserId,
                    MachineId = b.MachineId,
                    RentalHours = b.RentalHours,
                    TotalAmount = b.TotalAmount,
                    PaymentDate = b.PaymentDate,
                    PaymentStatus = b.PaymentStatus,
                    SlipImageUrl = b.SlipImageUrl,
                    RefundSlipImageUrl = b.RefundSlipImageUrl,
                    RefundReason = b.RefundReason,
                    RefundDate = b.RefundDate,
                    Note = b.Note,
                    StartTime = b.StartTime,
                    EndTime = b.EndTime,
                    User = _users.FirstOrDefault(u => u.UserId == b.UserId),
                    Machine = _machines.FirstOrDefault(m => m.MachineId == b.MachineId)
                };
            }
        }

        private static int _nextBookingId = 95000;

        public static Booking CreateNewBooking(int userId, int machineId, int rentalHours, decimal totalAmount, string? note = null, string? bookingDate = null, string? timeRange = null, IEnumerable<string>? slotList = null, string paymentStatus = "Approved", string? slipImageUrl = null)
        {
            lock (_lock)
            {
                var newId = ++_nextBookingId;
                var machine = _machines.FirstOrDefault(m => m.MachineId == machineId);
                if (machine != null && paymentStatus == "Approved")
                {
                    machine.Status = "Rented";
                }

                var booking = new Booking
                {
                    BookingId = newId,
                    UserId = userId,
                    MachineId = machineId,
                    RentalHours = rentalHours,
                    TotalAmount = totalAmount,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = paymentStatus,
                    SlipImageUrl = slipImageUrl,
                    StartTime = ParseBookingStartTime(bookingDate, timeRange) ?? DateTime.Now,
                    EndTime = ParseBookingEndTime(bookingDate, timeRange) ?? DateTime.Now.AddHours(rentalHours),
                    Note = note ?? $"จองและชำระ ({rentalHours} ชม.)",
                    User = _users.FirstOrDefault(u => u.UserId == userId),
                    Machine = machine
                };

                _bookings.Insert(0, booking);

                // บันทึกช่วงเวลาจองลง Registry เพื่อล็อกช่วงเวลานั้นบนวันที่ระบุทันที (ป้องกันการจองทับซ้อน)
                string dateToRegister = bookingDate ?? ExtractDateFromNote(note) ?? GetTodayThaiDate();
                if (slotList != null && slotList.Any())
                {
                    RegisterBookedSlotsList(machineId, dateToRegister, slotList, newId, userId);
                }
                else
                {
                    string timeRangeToRegister = timeRange ?? (note ?? "08:00 - 10:00");
                    RegisterBookedSlots(machineId, dateToRegister, timeRangeToRegister, newId, userId);
                }

                SaveToFile();
                return booking;
            }
        }

        // ==========================================
        // ระบบจัดการช่วงเวลาและสล็อตการจอง (Time Slot Scheduling & Overlap Prevention)
        // ==========================================
        public static readonly List<string> StandardDailySlots = new()
        {
            "08:00 - 09:00",
            "09:00 - 10:00",
            "10:00 - 11:00",
            "11:00 - 12:00",
            "12:00 - 13:00",
            "13:00 - 14:00",
            "14:00 - 15:00",
            "15:00 - 16:00",
            "16:00 - 17:00",
            "17:00 - 18:00",
            "18:00 - 19:00",
            "19:00 - 20:00",
            "20:00 - 21:00",
            "21:00 - 22:00",
            "22:00 - 23:00",
            "23:00 - 00:00"
        };

        public static string NormalizeDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return string.Empty;
            string d = dateStr.Trim();
            d = d.Replace("ต.ค.", "ตุลาคม")
                 .Replace("พ.ย.", "พฤศจิกายน")
                 .Replace("ธ.ค.", "ธันวาคม")
                 .Replace("ม.ค.", "มกราคม")
                 .Replace("ก.พ.", "กุมภาพันธ์")
                 .Replace("มี.ค.", "มีนาคม")
                 .Replace("เม.ย.", "เมษายน")
                 .Replace("พ.ค.", "พฤษภาคม")
                 .Replace("มิ.ย.", "มิถุนายน")
                 .Replace("ก.ค.", "กรกฎาคม")
                 .Replace("ส.ค.", "สิงหาคม")
                 .Replace("ก.ย.", "กันยายน");
            return d;
        }

        public static bool IsSameDate(string? d1, string? d2)
        {
            if (string.IsNullOrWhiteSpace(d1) && string.IsNullOrWhiteSpace(d2)) return true;
            if (string.IsNullOrWhiteSpace(d1) || string.IsNullOrWhiteSpace(d2)) return false;
            string n1 = NormalizeDate(d1);
            string n2 = NormalizeDate(d2);
            if (n1.Equals(n2, StringComparison.OrdinalIgnoreCase)) return true;

            var p1 = n1.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            var p2 = n2.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (p1.Length >= 2 && p2.Length >= 2)
            {
                bool dayMatches = p1[0] == p2[0];
                bool monthMatches = p1[1].Equals(p2[1], StringComparison.OrdinalIgnoreCase);
                return dayMatches && monthMatches;
            }
            return false;
        }

        public static string? ExtractDateFromNote(string? note)
        {
            if (string.IsNullOrWhiteSpace(note)) return null;
            if (note.Contains(","))
            {
                var parts = note.Split(',');
                return parts[0].Trim();
            }
            return null;
        }

        public static void RegisterBookedSlots(int machineId, string dateStr, string timeRange, int bookingId, int userId)
        {
            lock (_lock)
            {
                var slots = ParseSlotsFromTimeRange(timeRange);
                string cleanDate = string.IsNullOrWhiteSpace(dateStr) ? GetTodayThaiDate() : dateStr.Trim();
                foreach (var s in slots)
                {
                    if (!_bookedSlotsRegistry.Any(x => x.MachineId == machineId && IsSameDate(x.Date, cleanDate) && x.Slot.Equals(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        _bookedSlotsRegistry.Add(new BookedSlotInfo
                        {
                            MachineId = machineId,
                            Date = cleanDate,
                            Slot = s,
                            BookingId = bookingId,
                            UserId = userId
                        });
                    }
                }
            }
        }

        public static void RegisterBookedSlotsList(int machineId, string dateStr, IEnumerable<string> slots, int bookingId, int userId)
        {
            lock (_lock)
            {
                string cleanDate = string.IsNullOrWhiteSpace(dateStr) ? GetTodayThaiDate() : dateStr.Trim();
                foreach (var s in slots)
                {
                    if (!string.IsNullOrWhiteSpace(s) && !_bookedSlotsRegistry.Any(x => x.MachineId == machineId && IsSameDate(x.Date, cleanDate) && x.Slot.Equals(s, StringComparison.OrdinalIgnoreCase)))
                    {
                        _bookedSlotsRegistry.Add(new BookedSlotInfo
                        {
                            MachineId = machineId,
                            Date = cleanDate,
                            Slot = s.Trim(),
                            BookingId = bookingId,
                            UserId = userId
                        });
                    }
                }
                SaveToFile();
            }
        }

        public static void ReleaseBookedSlots(int bookingId)
        {
            lock (_lock)
            {
                _bookedSlotsRegistry.RemoveAll(x => x.BookingId == bookingId);
                SaveToFile();
            }
        }

        public static void SyncDbBookingSlot(int bookingId, int userId, int machineId, string? note)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(note)) return;
                string? dateStr = ExtractDateFromNote(note);
                if (!string.IsNullOrWhiteSpace(dateStr))
                {
                    RegisterBookedSlots(machineId, dateStr, note, bookingId, userId);
                }
            }
        }

        // ===== Parse StartTime/EndTime จาก bookingDate + timeRange =====
        // bookingDate เช่น "17 พฤศจิกายน 2569" หรือ "2026-11-17"
        // timeRange  เช่น "21:00 - 23:00" หรือ "21:00 - 23:00|..." (multi-slot)
        public static DateTime? ParseBookingStartTime(string? bookingDate, string? timeRange)
        {
            try
            {
                var date = ParseThaiOrIsoDate(bookingDate);
                if (date == null) return null;
                var startHHmm = ExtractStartHourFromRange(timeRange);
                if (startHHmm == null) return null;
                return date.Value.Date.Add(startHHmm.Value);
            }
            catch { return null; }
        }

        public static DateTime? ParseBookingEndTime(string? bookingDate, string? timeRange)
        {
            try
            {
                var date = ParseThaiOrIsoDate(bookingDate);
                if (date == null) return null;
                var endHHmm = ExtractEndHourFromRange(timeRange);
                if (endHHmm == null) return null;
                var endDt = date.Value.Date.Add(endHHmm.Value);
                // ถ้าเวลาสิ้นสุดข้ามวัน (เช่น 23:00 - 00:00) ให้เพิ่ม 1 วัน
                var startHHmm = ExtractStartHourFromRange(timeRange);
                if (startHHmm.HasValue && endHHmm.Value < startHHmm.Value)
                    endDt = endDt.AddDays(1);
                return endDt;
            }
            catch { return null; }
        }

        private static DateTime? ParseThaiOrIsoDate(string? dateStr)
        {
            if (string.IsNullOrWhiteSpace(dateStr)) return DateTime.Today;
            // ISO format: "2026-11-17"
            if (DateTime.TryParse(dateStr, out var iso)) return iso.Date;
            // ภาษาไทย เช่น "17 พฤศจิกายน 2569"
            var thaiMonths = new Dictionary<string, int>
            {
                {"มกราคม",1},{"กุมภาพันธ์",2},{"มีนาคม",3},{"เมษายน",4},
                {"พฤษภาคม",5},{"มิถุนายน",6},{"กรกฎาคม",7},{"สิงหาคม",8},
                {"กันยายน",9},{"ตุลาคม",10},{"พฤศจิกายน",11},{"ธันวาคม",12}
            };
            var parts = dateStr.Trim().Split(' ');
            if (parts.Length >= 3 && int.TryParse(parts[0], out int d) && thaiMonths.TryGetValue(parts[1], out int m) && int.TryParse(parts[2], out int y))
            {
                // แปลงปี พ.ศ. → ค.ศ.
                if (y > 2500) y -= 543;
                return new DateTime(y, m, d);
            }
            return DateTime.Today;
        }

        private static TimeSpan? ExtractStartHourFromRange(string? timeRange)
        {
            // "21:00 - 23:00" หรือ "21:00 - 23:00|..."
            var clean = (timeRange ?? "").Split('|')[0].Split(';')[0].Trim();
            var dash = clean.IndexOf(" - ");
            if (dash < 0) dash = clean.IndexOf("-");
            if (dash < 0) return null;
            var startStr = clean.Substring(0, dash).Trim();
            if (TimeSpan.TryParse(startStr, out var ts)) return ts;
            return null;
        }

        private static TimeSpan? ExtractEndHourFromRange(string? timeRange)
        {
            // ดึง slot สุดท้ายแล้วอ่าน end time
            var slots = (timeRange ?? "").Split(new[]{'|',';'}, StringSplitOptions.RemoveEmptyEntries);
            var lastSlot = slots.Length > 0 ? slots[slots.Length - 1].Trim() : (timeRange ?? "").Trim();
            var dash = lastSlot.IndexOf(" - ");
            if (dash < 0) dash = lastSlot.IndexOf("-");
            if (dash < 0) return null;
            var endStr = lastSlot.Substring(dash + 3).Trim();
            if (endStr.Contains("(")) endStr = endStr.Substring(0, endStr.IndexOf("(")).Trim();
            if (TimeSpan.TryParse(endStr, out var ts)) return ts;
            return null;
        }

        public static List<string> ParseSlotsFromTimeRange(string? timeRange)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(timeRange)) return result;

            // 1. ตรวจสอบว่าส่งมาเป็นรายการ slots คั่นด้วย '|' หรือ ';' หรือไม่ (เช่น "08:00 - 09:00|09:00 - 10:00|...")
            if (timeRange.Contains("|") || timeRange.Contains(";"))
            {
                var delims = new[] { '|', ';' };
                var rawSlots = timeRange.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                foreach (var s in rawSlots)
                {
                    var trimmed = s.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !result.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
                    {
                        result.Add(trimmed);
                    }
                }
                if (result.Any()) return result;
            }

            // 2. ล้างข้อมูลส่วนเกิน เช่น วงเล็บ (14 ชม.) หรือ วันที่ที่คั่นด้วย comma
            string clean = timeRange;
            if (clean.Contains("(")) clean = clean.Substring(0, clean.IndexOf("(")).Trim();
            if (clean.Contains(","))
            {
                var commaParts = clean.Split(',');
                clean = commaParts[commaParts.Length - 1].Trim();
            }

            // 3. แยกช่วงเวลาเริ่มและสิ้นสุด เช่น "08:00 - 00:00" หรือ "08:00 - 12:00"
            var parts = clean.Split(new[] { '-', '–' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                var s1 = parts[0].Trim().Replace("น.", "");
                var s2 = parts[1].Trim().Replace("น.", "");

                if (TimeSpan.TryParse(s1, out var t1) && TimeSpan.TryParse(s2, out var t2))
                {
                    // กรณีสิ้นสุดที่เที่ยงคืน 00:00 หรือ 24:00 หรือเวลาสิ้นสุด <= เวลาเริ่มต้น (สิ้นสุดวัน 24 ชั่วโมง)
                    if (s2 == "00:00" || s2 == "24:00" || t2 <= t1)
                    {
                        t2 = TimeSpan.FromHours(24);
                    }

                    var cur = t1;
                    while (cur < t2)
                    {
                        var nxt = cur.Add(TimeSpan.FromHours(1));
                        string curStr = $"{cur.Hours:D2}:{cur.Minutes:D2}";
                        string nxtStr = nxt.TotalHours >= 24 ? "00:00" : $"{nxt.Hours:D2}:{nxt.Minutes:D2}";
                        string slotStr = $"{curStr} - {nxtStr}";
                        if (!result.Contains(slotStr, StringComparer.OrdinalIgnoreCase))
                        {
                            result.Add(slotStr);
                        }
                        cur = nxt;
                    }
                }
            }
            return result;
        }

        public static List<string> GetBookedSlots(int machineId, string? dateStr = null)
        {
            lock (_lock)
            {
                string targetDate = string.IsNullOrWhiteSpace(dateStr) ? GetTodayThaiDate() : dateStr.Trim();
                var bookedSlots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // 1. ดึงจาก _bookedSlotsRegistry ที่ตรงกับวันที่ targetDate
                foreach (var item in _bookedSlotsRegistry)
                {
                    if (item.MachineId == machineId && IsSameDate(item.Date, targetDate))
                    {
                        bookedSlots.Add(item.Slot);
                    }
                }

                // 2. ดึงจาก _bookings ใน MockDataStore ที่ตรงกับวันที่ targetDate
                var activeBookings = _bookings.Where(b => b.MachineId == machineId && 
                    b.PaymentStatus != "Refunded" && 
                    b.PaymentStatus != "Rejected" && 
                    b.PaymentStatus != "RefundRequested" &&
                    b.PaymentStatus != "RefundRejected").ToList();

                foreach (var b in activeBookings)
                {
                    if (!string.IsNullOrEmpty(b.Note))
                    {
                        string? bDate = ExtractDateFromNote(b.Note);
                        if (!string.IsNullOrEmpty(bDate) && IsSameDate(bDate, targetDate))
                        {
                            var parsed = ParseSlotsFromTimeRange(b.Note);
                            foreach (var s in parsed) bookedSlots.Add(s);
                        }
                    }
                }

                // กรณีเครื่อง 2 (เครื่องทำงานตัดต่อ Render 02) คิวจองเริ่มต้นช่วง 11:00 - 13:00 สำหรับวันที่ 10 ตุลาคม 2566 และวันที่ปัจจุบัน
                if (machineId == 2 && (IsSameDate("10 ตุลาคม 2566", targetDate) || IsSameDate(GetTodayThaiDate(), targetDate)))
                {
                    bookedSlots.Add("11:00 - 12:00");
                    bookedSlots.Add("12:00 - 13:00");
                }

                return bookedSlots.ToList();
            }
        }

        public static List<BookedSlotInfo> GetBookedSlotsRegistry()
        {
            lock (_lock)
            {
                return _bookedSlotsRegistry.Select(s => new BookedSlotInfo
                {
                    MachineId = s.MachineId,
                    Date = s.Date,
                    Slot = s.Slot,
                    BookingId = s.BookingId,
                    UserId = s.UserId
                }).ToList();
            }
        }

        public static string GetMachineCategory(Machine? machine)
        {
            if (machine == null) return "Gaming Pro (RTX 4080 / RTX 4090)";
            var name = (machine.MachineName ?? "").ToLower();
            var gpu = (machine.GPU ?? "").ToLower();

            if (name.Contains("ai") || name.Contains("data") || gpu.Contains("a100") || gpu.Contains("h100") || 
                name.Contains("server") || name.Contains("เซิร์ฟเวอร์") || name.Contains("dev") || 
                name.Contains("cluster") || name.Contains("train") || name.Contains("ดีพเลิร์นนิง") || 
                name.Contains("ปัญญาประดิษฐ์") || name.Contains("epyc") || name.Contains("xeon"))
            {
                return "AI Cluster (EPYC / A100)";
            }

            if (name.Contains("render") || name.Contains("ตัดต่อ") || name.Contains("studio") || 
                name.Contains("ดีไซเนอร์") || name.Contains("graphic") || name.Contains("workstation") || 
                name.Contains("เวิร์กสเตชัน") || name.Contains("3d") || name.Contains("โมชันกราฟิก") || 
                name.Contains("วิดีโอโปรดักชัน"))
            {
                return "Graphic Studio (Render / 3D)";
            }

            if (name.Contains("office") || name.Contains("work") || name.Contains("สำนักงาน") || 
                name.Contains("เริ่มต้น") || name.Contains("ทั่วไป") || name.Contains("เอกสาร") || 
                name.Contains("พิมพ์งาน"))
            {
                return "Standard Office (Core i5 / Work)";
            }

            return "Gaming Pro (RTX 4080 / RTX 4090)";
        }

        public static void ApproveBooking(int id)
        {
            lock (_lock)
            {
                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                // Fallback: ถ้าหาด้วย ID ไม่เจอ ให้ค้นหารายการเติมเงินที่ยัง Pending อยู่
                if (b == null)
                {
                    b = _bookings.FirstOrDefault(x => x.PaymentStatus == "Pending" && (x.MachineId == 0 || (x.Note != null && x.Note.Contains("เติมเงิน"))));
                }

                if (b != null)
                {
                    b.PaymentStatus = "Approved";

                    // กรณีเป็นรายการเติมเงินเข้ากระเป๋าเงิน (Wallet Top-Up)
                    if (b.MachineId == 0 || (b.Note != null && b.Note.Contains("เติมเงิน")))
                    {
                        decimal current = GetUserBalance(b.UserId);
                        decimal updated = current + b.TotalAmount;
                        _userBalances[b.UserId] = updated;

                        // อัปเดตสถานะในประวัติธุรกรรม Wallet ให้เป็น "สำเร็จ"
                        var txn = _walletTransactions.FirstOrDefault(t => 
                            t.ReferenceCode == $"#TOPUP-{b.BookingId}" || 
                            (t.UserId == b.UserId && t.Amount == b.TotalAmount && t.Status == "รอตรวจสอบ") ||
                            (t.Status == "รอตรวจสอบ")
                        );
                        if (txn != null)
                        {
                            txn.Status = "สำเร็จ";
                            txn.Title = "เติมเงินเข้ากระเป๋าเงิน (ฝ่ายบัญชีอนุมัติแล้ว)";
                        }
                        else
                        {
                            _walletTransactions.Insert(0, new WalletTransaction
                            {
                                Id = ++_nextTxnId,
                                UserId = b.UserId,
                                CreatedAt = DateTime.Now,
                                Title = "เติมเงินเข้ากระเป๋าเงิน (ฝ่ายบัญชีอนุมัติแล้ว)",
                                ReferenceCode = $"#TOPUP-{b.BookingId}",
                                Channel = "พร้อมเพย์ QR Code (061-201-3936)",
                                Amount = b.TotalAmount,
                                Status = "สำเร็จ"
                            });
                        }
                    }
                    else
                    {
                        // ปรับสถานะเครื่องเป็น Rented
                        var m = _machines.FirstOrDefault(x => x.MachineId == b.MachineId);
                        if (m != null && m.Status == "Available")
                        {
                            m.Status = "Rented";
                        }

                        // มอบคะแนนสะสมจากการเช่าเครื่อง
                        AwardBookingPointsInternal(b.UserId, b.TotalAmount, m?.MachineName ?? "Cloud PC", b.RentalHours, b.BookingId);
                    }
                    SaveToFile();
                }
            }
        }

        public static void RejectBooking(int id, string? note)
        {
            lock (_lock)
            {
                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                if (b != null)
                {
                    b.PaymentStatus = "Rejected";
                    b.Note = string.IsNullOrWhiteSpace(note) ? "สลิปไม่ถูกต้อง หรือยอดเงินไม่ตรงกับรายการ" : note;

                    // กรณีเป็นรายการเติมเงินเข้า Wallet
                    if (b.MachineId == 0 || (b.Note != null && b.Note.Contains("เติมเงิน")))
                    {
                        var txn = _walletTransactions.FirstOrDefault(t => t.ReferenceCode == $"#TOPUP-{b.BookingId}" || (t.UserId == b.UserId && t.Amount == b.TotalAmount && t.Status == "รอตรวจสอบ"));
                        if (txn != null)
                        {
                            txn.Status = "ปฏิเสธ (สลิปไม่ผ่าน)";
                            txn.Title = $"เติมเงินไม่สำเร็จ ({b.Note})";
                        }
                    }
                    else
                    {
                        // ปลดล็อกสล็อตเวลาเพื่อให้ผู้ใช้อื่นจองได้
                        _bookedSlotsRegistry.RemoveAll(x => x.BookingId == id);
                    }
                    SaveToFile();
                }
            }
        }

        public static void ProcessRefundBooking(int id, string? refundSlipUrl, string? note)
        {
            lock (_lock)
            {
                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                if (b != null)
                {
                    b.PaymentStatus = "Refunded";
                    b.RefundSlipImageUrl = refundSlipUrl ?? b.RefundSlipImageUrl;
                    b.RefundDate = DateTime.Now;
                    if (!string.IsNullOrWhiteSpace(note)) b.Note = note;

                    ReleaseBookedSlots(b.BookingId);

                    // ปรับสถานะเครื่องกลับเป็น Available
                    var m = _machines.FirstOrDefault(x => x.MachineId == b.MachineId);
                    if (m != null && m.Status == "Rented")
                    {
                        m.Status = "Available";
                    }
                    SaveToFile();
                }
            }
        }

        public static void ApproveRefundBooking(int id, string? note)
        {
            lock (_lock)
            {
                string finalNote = string.IsNullOrWhiteSpace(note) 
                    ? "ตรวจสอบเงื่อนไขตรงตามนโยบาย อนุมัติคืนเงินเต็มจำนวนเรียบร้อย (+60.00 ฿ เข้ากระเป๋าเงิน)" 
                    : note;

                ReleaseBookedSlots(id);

                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                if (b != null)
                {
                    b.PaymentStatus = "Refunded";
                    b.RefundDate = DateTime.Now;
                    b.Note = finalNote;

                    ReleaseBookedSlots(b.BookingId);

                    // ปรับสถานะเครื่องกลับเป็น Available
                    var m = _machines.FirstOrDefault(x => x.MachineId == b.MachineId);
                    if (m != null && m.Status == "Rented")
                    {
                        m.Status = "Available";
                    }
                }

                // ซิงค์ข้อมูลให้ทั้งคำขอ 103, 104 และ 94812 เฉพาะกลุ่ม seed data อัปเดตพร้อมกัน
                if (id == 103 || id == 104 || id == 94812)
                {
                    var thanapolBookings = _bookings.Where(x => x.BookingId == 103 || x.BookingId == 104 || x.BookingId == 94812).ToList();
                    foreach (var tb in thanapolBookings)
                    {
                        tb.PaymentStatus = "Refunded";
                        tb.RefundDate = DateTime.Now;
                        tb.Note = finalNote;
                    }
                }
                SaveToFile();
            }
        }

        public static void RejectRefundBooking(int id, string? note)
        {
            lock (_lock)
            {
                string finalNote = string.IsNullOrWhiteSpace(note) 
                    ? "ตรวจสอบแล้วไม่เข้าเงื่อนไขการคืนเงินของบริษัท" 
                    : note;

                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                if (b != null)
                {
                    b.PaymentStatus = "RefundRejected";
                    b.RefundDate = DateTime.Now;
                    b.Note = finalNote;
                }

                if (id == 103 || id == 104 || id == 94812)
                {
                    var thanapolBookings = _bookings.Where(x => x.BookingId == 103 || x.BookingId == 104 || x.BookingId == 94812).ToList();
                    foreach (var tb in thanapolBookings)
                    {
                        tb.PaymentStatus = "RefundRejected";
                        tb.RefundDate = DateTime.Now;
                        tb.Note = finalNote;
                    }
                }
                SaveToFile();
            }
        }

        public static bool RequestRefundBooking(int id, string refundReason, int? userId = null)
        {
            lock (_lock)
            {
                var b = _bookings.FirstOrDefault(x => x.BookingId == id);
                if (b == null && _bookings.Any())
                {
                    b = _bookings.FirstOrDefault(x => (userId == null || x.UserId == userId) && x.PaymentStatus == "Approved");
                }
                if (b == null && _bookings.Any())
                {
                    b = _bookings.FirstOrDefault(x => x.UserId == (userId ?? 5));
                }

                if (b != null)
                {
                    b.PaymentStatus = "RefundRequested";
                    b.RefundReason = refundReason;
                    b.RefundDate = DateTime.Now;

                    if (id == 103 || id == 104 || id == 94812)
                    {
                        var thanapolBookings = _bookings.Where(x => x.BookingId == 103 || x.BookingId == 104 || x.BookingId == 94812).ToList();
                        foreach (var tb in thanapolBookings)
                        {
                            tb.PaymentStatus = "RefundRequested";
                            tb.RefundReason = refundReason;
                            tb.RefundDate = DateTime.Now;
                        }
                    }

                    SaveToFile();
                    return true;
                }
                else
                {
                    _bookings.Insert(0, new Booking
                    {
                        BookingId = id,
                        UserId = userId ?? 5,
                        MachineId = 1,
                        RentalHours = 4,
                        TotalAmount = 60.00m,
                        PaymentDate = DateTime.Now,
                        PaymentStatus = "RefundRequested",
                        RefundReason = refundReason,
                        RefundDate = DateTime.Now,
                        Note = $"#CPC-{id:D5} (ลูกค้ายื่นคำขอคืนเงินผ่านหน้าเว็บ)"
                    });
                    SaveToFile();
                    return true;
                }
            }
        }

        // ==========================================
        // ระบบจัดการกระเป๋าเงินและเครดิตเรียลไทม์ (Real-time Wallet)
        // ==========================================
        public static decimal GetUserBalance(int userId)
        {
            if (userId == 5 && !_userBalances.ContainsKey(5))
            {
                _userBalances[5] = 600.00m;
            }
            return _userBalances.GetOrAdd(userId, 0.00m);
        }

        public static void SetUserBalance(int userId, decimal balance)
        {
            _userBalances[userId] = balance;
            SaveToFile();
        }

        public static Booking CreateTopUpRequest(int userId, decimal amount, string? slipUrl)
        {
            lock (_lock)
            {
                int bookingId = ++_nextBookingId;
                var user = _users.FirstOrDefault(u => u.UserId == userId);
                var b = new Booking
                {
                    BookingId = bookingId,
                    UserId = userId,
                    User = user,
                    MachineId = 0,
                    RentalHours = 0,
                    TotalAmount = amount,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = "Pending",
                    SlipImageUrl = slipUrl,
                    StartTime = DateTime.Now,
                    EndTime = DateTime.Now,
                    Note = "เติมเงินเข้ากระเป๋าเงิน CloudPC Wallet (รอฝ่ายบัญชีตรวจสอบสลิป)"
                };
                _bookings.Insert(0, b);

                // บันทึกธุรกรรมรอตรวจสอบในประวัติกระเป๋าเงิน
                _walletTransactions.Insert(0, new WalletTransaction
                {
                    Id = ++_nextTxnId,
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    Title = "แจ้งเติมเงินเข้ากระเป๋าเงิน (รอฝ่ายบัญชีตรวจสอบสลิป)",
                    ReferenceCode = $"#TOPUP-{bookingId}",
                    Channel = "พร้อมเพย์ QR Code (061-201-3936)",
                    Amount = amount,
                    Status = "รอตรวจสอบ"
                });

                SaveToFile();
                return b;
            }
        }

        public static decimal TopUpBalance(int userId, decimal amount, string channel)
        {
            lock (_lock)
            {
                decimal current = GetUserBalance(userId);
                decimal updated = current + amount;
                _userBalances[userId] = updated;

                _walletTransactions.Insert(0, new WalletTransaction
                {
                    Id = ++_nextTxnId,
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    Title = "เติมเงินเข้ากระเป๋าเงิน",
                    ReferenceCode = $"#TOPUP-{DateTime.Now:yyyyMMdd}-{_nextTxnId:D2}",
                    Channel = channel,
                    Amount = amount,
                    Status = "สำเร็จ"
                });

                SaveToFile();
                return updated;
            }
        }

        public static bool DeductBalance(int userId, decimal amount, string title, string refCode)
        {
            lock (_lock)
            {
                decimal current = GetUserBalance(userId);
                if (current < amount) return false;

                decimal updated = current - amount;
                _userBalances[userId] = updated;

                _walletTransactions.Insert(0, new WalletTransaction
                {
                    Id = ++_nextTxnId,
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    Title = title,
                    ReferenceCode = refCode,
                    Channel = "กระเป๋าเงิน CloudPC",
                    Amount = -amount,
                    Status = "สำเร็จ"
                });

                SaveToFile();
                return true;
            }
        }

        public static decimal RefundBalance(int userId, decimal amount, string title, string refCode)
        {
            lock (_lock)
            {
                decimal current = GetUserBalance(userId);
                decimal updated = current + amount;
                _userBalances[userId] = updated;

                _walletTransactions.Insert(0, new WalletTransaction
                {
                    Id = ++_nextTxnId,
                    UserId = userId,
                    CreatedAt = DateTime.Now,
                    Title = title,
                    ReferenceCode = refCode,
                    Channel = "คืนเงินเข้ากระเป๋าเงิน CloudPC",
                    Amount = amount,
                    Status = "สำเร็จ"
                });

                SaveToFile();
                return updated;
            }
        }

        public static List<WalletTransaction> GetUserTransactions(int userId)
        {
            lock (_lock)
            {
                var list = _walletTransactions.Where(t => t.UserId == userId).ToList();
                if (!list.Any())
                {
                    // กรณีเป็น User ใหม่ คืนค่าประวัติจำลองเริ่มต้นของ thanapol (userId 5)
                    return _walletTransactions.Where(t => t.UserId == 5).ToList();
                }
                return list;
            }
        }

        // ========================================================
        // ระบบสมาชิกและคะแนนสะสม (Membership & Points System)
        // ========================================================
        public static void EnsureUserPointsAndHistory()
        {
            lock (_lock)
            {
                // กำหนดคะแนนพื้นฐานของ thanapol (userId: 5) เริ่มต้นที่ 1,250 PTS
                if (!_userPoints.ContainsKey(5))
                {
                    _userPoints[5] = 1250;
                }

                if (!_pointTransactions.Any(t => t.UserId == 5))
                {
                    _pointTransactions.AddRange(new List<PointTransaction>
                    {
                        new PointTransaction
                        {
                            Id = 1,
                            UserId = 5,
                            ReferenceCode = "#PTS-98402",
                            CreatedAt = DateTime.Now.AddDays(-22),
                            Description = "รับคะแนนสะสมจากการเช่าเครื่อง Extreme Gaming PC (4 ชม.)",
                            SubDescription = "ระดับ Platinum (คูณพิเศษ 1.5 เท่า)",
                            Points = 120,
                            BalanceAfter = 1250,
                            Type = "receive"
                        },
                        new PointTransaction
                        {
                            Id = 2,
                            UserId = 5,
                            ReferenceCode = "#PTS-96531",
                            CreatedAt = DateTime.Now.AddDays(-27),
                            Description = "แลกคะแนนเพื่อส่วนลดค่าเช่าเครื่องครั้งถัดไป - ส่วนลด 100 THB",
                            SubDescription = "ใช้คูปองส่วนลดในหน้าเช่าเครื่อง",
                            Points = -500,
                            BalanceAfter = 1130,
                            Type = "spend"
                        },
                        new PointTransaction
                        {
                            Id = 3,
                            UserId = 5,
                            ReferenceCode = "#PTS-95410",
                            CreatedAt = DateTime.Now.AddDays(-32),
                            Description = "รับคะแนนสะสมจากการเช่าเครื่อง High-End Workstation (5 ชม.)",
                            SubDescription = "การเช่าเสร็จสิ้นสมบูรณ์",
                            Points = 150,
                            BalanceAfter = 1630,
                            Type = "receive"
                        },
                        new PointTransaction
                        {
                            Id = 4,
                            UserId = 5,
                            ReferenceCode = "#PTS-93218",
                            CreatedAt = DateTime.Now.AddDays(-37),
                            Description = "รับคะแนนโบนัสพิเศษเนื่องจากเลื่อนขึ้นสู่ระดับสมาชิก Platinum",
                            SubDescription = "โบนัสฉลองเลื่อนระดับอัตโนมัติ",
                            Points = 300,
                            BalanceAfter = 1480,
                            Type = "receive"
                        }
                    });
                }

                // ซิงค์การเช่าเครื่องที่อนุมัติแล้วใน _bookings ของลูกค้า (หากยังไม่ได้รับคะแนน)
                var approvedRentals = _bookings.Where(b => b.MachineId > 0 && b.PaymentStatus == "Approved")
                                              .OrderBy(b => b.PaymentDate)
                                              .ToList();

                foreach (var b in approvedRentals)
                {
                    string refCode = $"#PTS-{b.BookingId}";
                    if (!_pointTransactions.Any(t => t.ReferenceCode == refCode && t.UserId == b.UserId))
                    {
                        var mName = b.Machine?.MachineName ?? _machines.FirstOrDefault(m => m.MachineId == b.MachineId)?.MachineName ?? "Cloud PC";
                        AwardBookingPointsInternal(b.UserId, b.TotalAmount, mName, b.RentalHours, b.BookingId, b.PaymentDate);
                    }
                }
            }
        }

        private static int AwardBookingPointsInternal(int userId, decimal amountPaid, string machineName, int hours, int bookingId, DateTime? date = null)
        {
            string refCode = $"#PTS-{bookingId}";
            if (_pointTransactions.Any(t => t.ReferenceCode == refCode && t.UserId == userId))
            {
                return 0;
            }

            int currentPts = GetUserPoints(userId);
            double multiplier = 1.0;
            string tier = "Bronze";
            if (currentPts >= 2500) { multiplier = 2.0; tier = "Diamond"; }
            else if (currentPts >= 1200) { multiplier = 1.5; tier = "Platinum"; }
            else if (currentPts >= 700) { multiplier = 1.3; tier = "Gold"; }
            else if (currentPts >= 300) { multiplier = 1.2; tier = "Silver"; }

            // กฎ: ทุก 10 บาท ได้ 1 คะแนน พร้อมตัวคูณระดับ
            int earned = (int)Math.Round((double)amountPaid / 10.0 * multiplier);
            if (earned < 1 && amountPaid > 0) earned = 1;
            if (amountPaid == 0) earned = 5; // โบนัสสำหรับการเช่าโปรโมชันฟรี

            int newTotal = currentPts + earned;
            _userPoints[userId] = newTotal;

            var txn = new PointTransaction
            {
                Id = ++_nextPointTxnId,
                UserId = userId,
                ReferenceCode = refCode,
                CreatedAt = date ?? DateTime.Now,
                Description = $"รับคะแนนสะสมจากการเช่าเครื่อง {machineName} ({hours} ชม.)",
                SubDescription = $"ระดับ {tier} (คูณพิเศษ {multiplier:0.0} เท่า - ยอดชำระ {amountPaid:N2} ฿)",
                Points = earned,
                BalanceAfter = newTotal,
                Type = "receive"
            };
            _pointTransactions.Insert(0, txn);
            return earned;
        }

        public static int AwardBookingPoints(int userId, decimal amountPaid, string machineName, int hours, int bookingId)
        {
            lock (_lock)
            {
                EnsureUserPointsAndHistory();
                int earned = AwardBookingPointsInternal(userId, amountPaid, machineName, hours, bookingId);
                SaveToFile();
                return earned;
            }
        }

        public static int GetUserPoints(int userId)
        {
            if (!_userPoints.ContainsKey(userId))
            {
                _userPoints[userId] = (userId == 5) ? 1250 : 0;
            }
            return _userPoints[userId];
        }

        public static void SetUserPoints(int userId, int points)
        {
            _userPoints[userId] = points;
            SaveToFile();
        }

        public static UserMembershipInfo GetUserMembershipInfo(int userId, string? username = null)
        {
            lock (_lock)
            {
                EnsureUserPointsAndHistory();

                int points = GetUserPoints(userId);
                string tierName;
                int tierLevel;
                double multiplier;
                int nextTierPts;
                string nextTierName;
                double progressPercent;

                if (points < 300)
                {
                    tierName = "Bronze";
                    tierLevel = 1;
                    multiplier = 1.0;
                    nextTierPts = 300;
                    nextTierName = "Silver";
                    progressPercent = Math.Round(((double)points / 300.0) * 100);
                }
                else if (points < 700)
                {
                    tierName = "Silver";
                    tierLevel = 2;
                    multiplier = 1.2;
                    nextTierPts = 700;
                    nextTierName = "Gold";
                    progressPercent = Math.Round(((double)(points - 300) / (700.0 - 300.0)) * 100);
                }
                else if (points < 1200)
                {
                    tierName = "Gold";
                    tierLevel = 3;
                    multiplier = 1.3;
                    nextTierPts = 1200;
                    nextTierName = "Platinum";
                    progressPercent = Math.Round(((double)(points - 700) / (1200.0 - 700.0)) * 100);
                }
                else if (points < 2500)
                {
                    tierName = "Platinum";
                    tierLevel = 4;
                    multiplier = 1.5;
                    nextTierPts = 2500;
                    nextTierName = "Diamond";
                    progressPercent = Math.Round(((double)(points - 1200) / (2500.0 - 1200.0)) * 100);
                }
                else
                {
                    tierName = "Diamond";
                    tierLevel = 5;
                    multiplier = 2.0;
                    nextTierPts = 2500;
                    nextTierName = "Diamond (ระดับสูงสุด)";
                    progressPercent = 100;
                }

                progressPercent = Math.Clamp(progressPercent, 0, 100);

                var txns = _pointTransactions.Where(t => t.UserId == userId)
                                            .OrderByDescending(t => t.CreatedAt)
                                            .ToList();

                var userObj = _users.FirstOrDefault(u => u.UserId == userId);
                string memberIdDisplay = (userId == 5) ? "CPC-77491" : $"CPC-{userId * 15498 % 90000 + 10000}";
                string displayName = !string.IsNullOrWhiteSpace(username) ? username : (userObj?.Username ?? "ธนพล ดีสมเกียรติ");
                if (displayName.Equals("thanapol", StringComparison.OrdinalIgnoreCase))
                {
                    displayName = "ธนพล ดีสมเกียรติ";
                }

                int expiringPoints = points > 0 ? (userId == 5 ? 150 : Math.Min(points, 50)) : 0;
                string expiringDateDisplay = $"31 ธ.ค. {DateTime.Now.Year + 543 + 1}";

                return new UserMembershipInfo
                {
                    UserId = userId,
                    MemberNumber = memberIdDisplay,
                    FullName = displayName,
                    CurrentPoints = points,
                    ExpiringPoints = expiringPoints,
                    ExpiringDate = expiringDateDisplay,
                    TierName = tierName,
                    TierLevel = tierLevel,
                    Multiplier = multiplier,
                    NextTierPoints = nextTierPts,
                    NextTierName = nextTierName,
                    ProgressPercentage = progressPercent,
                    Transactions = txns
                };
            }
        }

        // ========================================================
        // Support Tickets & Live Chat System
        // ========================================================
        public static List<SupportTicket> GetSupportTickets(int? userId = null)
        {
            lock (_lock)
            {
                EnsureDefaultTickets();
                if (userId.HasValue)
                {
                    return _supportTickets.Where(t => t.UserId == userId.Value)
                                          .OrderByDescending(t => t.UpdatedAt)
                                          .ToList();
                }
                return _supportTickets.OrderByDescending(t => t.UpdatedAt).ToList();
            }
        }

        public static SupportTicket? GetSupportTicketById(int ticketId)
        {
            lock (_lock)
            {
                EnsureDefaultTickets();
                return _supportTickets.FirstOrDefault(t => t.TicketId == ticketId);
            }
        }

        public static SupportTicket CreateSupportTicket(int userId, string userName, string userEmail, string subject, string category, string initialMessage, int? bookingId = null, string? machineName = null)
        {
            lock (_lock)
            {
                EnsureDefaultTickets();
                var ticket = new SupportTicket
                {
                    TicketId = _nextTicketId++,
                    TicketCode = $"#TK-{_nextTicketId - 1}",
                    UserId = userId,
                    UserName = userName,
                    UserEmail = userEmail,
                    Subject = subject,
                    Category = category,
                    RelatedBookingId = bookingId,
                    RelatedMachineName = machineName,
                    Status = "WaitingAdmin",
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Messages = new List<SupportMessage>()
                };

                if (!string.IsNullOrWhiteSpace(initialMessage))
                {
                    ticket.Messages.Add(new SupportMessage
                    {
                        MessageId = _nextMessageId++,
                        TicketId = ticket.TicketId,
                        SenderUserId = userId,
                        SenderName = userName,
                        SenderRole = "Customer",
                        Content = initialMessage,
                        SentAt = DateTime.Now,
                        IsAdminReply = false
                    });
                }

                _supportTickets.Insert(0, ticket);
                SaveToFile();
                return ticket;
            }
        }

        public static SupportMessage AddMessageToTicket(int ticketId, int senderUserId, string senderName, string senderRole, string content, bool isAdmin)
        {
            lock (_lock)
            {
                EnsureDefaultTickets();
                var ticket = _supportTickets.FirstOrDefault(t => t.TicketId == ticketId);
                if (ticket == null)
                {
                    ticket = CreateSupportTicket(senderUserId, senderName, "customer@example.com", "แจ้งปัญหาการใช้งาน", "ทั่วไป", content);
                    return ticket.Messages.First();
                }

                var msg = new SupportMessage
                {
                    MessageId = _nextMessageId++,
                    TicketId = ticketId,
                    SenderUserId = senderUserId,
                    SenderName = senderName,
                    SenderRole = senderRole,
                    Content = content,
                    SentAt = DateTime.Now,
                    IsAdminReply = isAdmin
                };

                ticket.Messages.Add(msg);
                ticket.UpdatedAt = DateTime.Now;
                ticket.Status = isAdmin ? "Answered" : "WaitingAdmin";

                SaveToFile();
                return msg;
            }
        }

        public static void UpdateSupportTicketStatus(int ticketId, string status)
        {
            lock (_lock)
            {
                EnsureDefaultTickets();
                var ticket = _supportTickets.FirstOrDefault(t => t.TicketId == ticketId);
                if (ticket != null)
                {
                    ticket.Status = status;
                    ticket.UpdatedAt = DateTime.Now;
                    SaveToFile();
                }
            }
        }

        public static void EnsureDefaultTickets()
        {
            if (_supportTickets.Any()) return;

            var ticket1 = new SupportTicket
            {
                TicketId = 1001,
                TicketCode = "#TK-1001",
                UserId = 991,
                UserName = "ธนพล ดีสมเกียรติ",
                UserEmail = "thanapol@example.com",
                Subject = "สอบถามการปรับความเร็วสตรีมมิ่งเครื่องเล่นเกม",
                Category = "การเชื่อมต่อ & รีโมท",
                RelatedBookingId = 94812,
                RelatedMachineName = "Extreme Gaming PC - RTX 4090",
                Status = "Answered",
                CreatedAt = DateTime.Now.AddHours(-2),
                UpdatedAt = DateTime.Now.AddMinutes(-25),
                Messages = new List<SupportMessage>
                {
                    new SupportMessage
                    {
                        MessageId = 5001,
                        TicketId = 1001,
                        SenderUserId = 991,
                        SenderName = "ธนพล ดีสมเกียรติ",
                        SenderRole = "Customer",
                        Content = "สวัสดีครับแอดมิน เครื่อง Extreme Gaming PC (#CPC-94812) ตอนนี้สตรีมภาพแล้วมีอาการหน่วงและดีเลย์นิดหน่อยครับ สามารถปรับความเร็วเพิ่มได้ไหมครับ",
                        SentAt = DateTime.Now.AddHours(-2),
                        IsAdminReply = false
                    },
                    new SupportMessage
                    {
                        MessageId = 5002,
                        TicketId = 1001,
                        SenderUserId = 1,
                        SenderName = "สมชาย (แอดมินดูแลระบบ)",
                        SenderRole = "Admin",
                        Content = "สวัสดีครับคุณธนพล ทางแอดมินได้ทำการสลับช่องทาง Streaming Route ความหน่วงต่ำพิเศษ Ultra-low Latency พร้อมเปิดแบนด์วิดท์ 1Gbps ให้แล้วครับ รบกวนกด Reconnect ใหม่อีกครั้งได้เลยครับผม",
                        SentAt = DateTime.Now.AddHours(-1).AddMinutes(-50),
                        IsAdminReply = true
                    },
                    new SupportMessage
                    {
                        MessageId = 5003,
                        TicketId = 1001,
                        SenderUserId = 991,
                        SenderName = "ธนพล ดีสมเกียรติ",
                        SenderRole = "Customer",
                        Content = "ขอบคุณมากครับแอดมิน ภาพลื่นขึ้นทันทีเลยครับ 60fps นิ่งๆ สบายมาก!",
                        SentAt = DateTime.Now.AddHours(-1).AddMinutes(-40),
                        IsAdminReply = false
                    },
                    new SupportMessage
                    {
                        MessageId = 5004,
                        TicketId = 1001,
                        SenderUserId = 1,
                        SenderName = "สมชาย (แอดมินดูแลระบบ)",
                        SenderRole = "Admin",
                        Content = "ยินดีให้บริการครับคุณธนพล หากพบปัญหาหรือต้องการความช่วยเหลือเพิ่มเติม สามารถพิมพ์แจ้งในแชทนี้ได้ตลอด 24 ชั่วโมงนะครับ",
                        SentAt = DateTime.Now.AddMinutes(-25),
                        IsAdminReply = true
                    }
                }
            };

            var ticket2 = new SupportTicket
            {
                TicketId = 1002,
                TicketCode = "#TK-1002",
                UserId = 992,
                UserName = "สมชาย ลูกค้าประจำ",
                UserEmail = "somchai@example.com",
                Subject = "สอบถามการต่อเวลาเช่าเครื่องล่วงหน้า",
                Category = "การจอง & ชำระเงิน",
                RelatedBookingId = 93721,
                RelatedMachineName = "Standard Office PC - Core i5",
                Status = "WaitingAdmin",
                CreatedAt = DateTime.Now.AddMinutes(-45),
                UpdatedAt = DateTime.Now.AddMinutes(-45),
                Messages = new List<SupportMessage>
                {
                    new SupportMessage
                    {
                        MessageId = 5005,
                        TicketId = 1002,
                        SenderUserId = 992,
                        SenderName = "สมชาย ลูกค้าประจำ",
                        SenderRole = "Customer",
                        Content = "รบกวนสอบถามครับ ถ้าต้องการต่อเวลาใช้งานเพิ่มอีก 2 ชั่วโมง สามารถกดหักเงินจาก CloudPC Wallet ต่อเวลาเครื่องได้ทันทีเลยไหมครับ หรือต้องจองใหม่ครับ",
                        SentAt = DateTime.Now.AddMinutes(-45),
                        IsAdminReply = false
                    }
                }
            };

            _supportTickets.Add(ticket1);
            _supportTickets.Add(ticket2);
        }

        // ==========================================
        // System Logs & Audit Logging System
        // ==========================================
        public static void EnsureDefaultLogs()
        {
            if (_systemLogs.Any()) return;

            DateTime now = DateTime.Now;
            _systemLogs.AddRange(new List<SystemLog>
            {
                new SystemLog
                {
                    LogId = 1,
                    Timestamp = now.AddMinutes(-12),
                    PerformedBy = "admin (ผู้ดูแลระบบหลัก)",
                    Category = "Security",
                    Detail = "ตรวจสอบและปรับปรุงนโยบายรหัสผ่านและความปลอดภัยของระบบ",
                    Severity = "สำคัญ",
                    IpAddress = "192.168.1.50"
                },
                new SystemLog
                {
                    LogId = 2,
                    Timestamp = now.AddMinutes(-35),
                    PerformedBy = "admin",
                    Category = "Login",
                    Detail = "ผู้ดูแลระบบหลัก (admin) เข้าสู่ระบบสำเร็จ",
                    Severity = "ข้อมูลทั่วไป",
                    IpAddress = "192.168.1.50"
                },
                new SystemLog
                {
                    LogId = 3,
                    Timestamp = now.AddHours(-1).AddMinutes(-10),
                    PerformedBy = "System Monitor",
                    Category = "Hardware",
                    Detail = "ตรวจพบเครื่อง PC-001 มีการใช้งานทรัพยากร GPU สูงต่อเนื่อง อุณหภูมิปกติ",
                    Severity = "ข้อมูลทั่วไป",
                    IpAddress = "10.0.0.1"
                },
                new SystemLog
                {
                    LogId = 4,
                    Timestamp = now.AddHours(-2).AddMinutes(-5),
                    PerformedBy = "accountant",
                    Category = "Payment",
                    Detail = "ตรวจสอบและยืนยันการคืนเงินค่าเช่าเครื่องเข้า CloudPC Wallet เรียบร้อย",
                    Severity = "สำคัญ",
                    IpAddress = "192.168.1.104"
                },
                new SystemLog
                {
                    LogId = 5,
                    Timestamp = now.AddHours(-3).AddMinutes(-20),
                    PerformedBy = "manager",
                    Category = "Security",
                    Detail = "ผู้จัดการสาขา (manager) ตรวจสอบรายงานประสิทธิภาพและสถิติการใช้งานประจำวัน",
                    Severity = "ข้อมูลทั่วไป",
                    IpAddress = "192.168.1.102"
                },
                new SystemLog
                {
                    LogId = 6,
                    Timestamp = now.AddHours(-4).AddMinutes(-30),
                    PerformedBy = "Database AutoBackup",
                    Category = "System",
                    Detail = "สำรองข้อมูลฐานข้อมูล CloudPC (csi402db / MockDataStore) สำเร็จเรียบร้อย",
                    Severity = "ข้อมูลทั่วไป",
                    IpAddress = "127.0.0.1"
                },
                new SystemLog
                {
                    LogId = 7,
                    Timestamp = now.AddDays(-1).Date.AddHours(16).AddMinutes(15),
                    PerformedBy = "staff",
                    Category = "Hardware",
                    Detail = "พนักงานหน้าร้านตรวจสอบสภาพเครื่อง PC-001 ถึง PC-022 พร้อมให้บริการ",
                    Severity = "ข้อมูลทั่วไป",
                    IpAddress = "192.168.1.103"
                },
                new SystemLog
                {
                    LogId = 8,
                    Timestamp = now.AddDays(-1).Date.AddHours(14).AddMinutes(20),
                    PerformedBy = "System Monitor",
                    Category = "Hardware",
                    Detail = "เครื่อง PC-005 ถูกตั้งค่าเข้าสู่โหมดซ่อมบำรุงตามการแจ้งของช่างเทคนิค",
                    Severity = "วิกฤต",
                    IpAddress = "10.0.0.5"
                }
            });
            _nextLogId = 100;
        }

        public static void LogActivity(string performedBy, string category, string detail, string severity = "ข้อมูลทั่วไป", string? ipAddress = null)
        {
            lock (_lock)
            {
                EnsureDefaultLogs();
                var log = new SystemLog
                {
                    LogId = _nextLogId++,
                    Timestamp = DateTime.Now,
                    PerformedBy = string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy.Trim(),
                    Category = string.IsNullOrWhiteSpace(category) ? "System" : category.Trim(),
                    Detail = detail ?? string.Empty,
                    Severity = string.IsNullOrWhiteSpace(severity) ? "ข้อมูลทั่วไป" : severity.Trim(),
                    IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress.Trim()
                };
                _systemLogs.Insert(0, log);
                SaveToFile();
            }
        }

        public static List<SystemLog> GetSystemLogs(DateTime? date = null, string? search = null, string? category = null, string? severity = null)
        {
            lock (_lock)
            {
                EnsureDefaultLogs();
                var query = _systemLogs.AsEnumerable();

                if (date.HasValue)
                {
                    query = query.Where(l => l.Timestamp.Date == date.Value.Date);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    string kw = search.Trim().ToLower();
                    query = query.Where(l => (l.PerformedBy != null && l.PerformedBy.ToLower().Contains(kw)) ||
                                             (l.Detail != null && l.Detail.ToLower().Contains(kw)) ||
                                             (l.IpAddress != null && l.IpAddress.ToLower().Contains(kw)));
                }

                if (!string.IsNullOrWhiteSpace(category) && !category.Equals("ประเภททั้งหมด", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(l => l.Category.Equals(category.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                if (!string.IsNullOrWhiteSpace(severity) && !severity.Equals("ระดับความรุนแรงทั้งหมด", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(l => l.Severity.Equals(severity.Trim(), StringComparison.OrdinalIgnoreCase));
                }

                return query.OrderByDescending(l => l.Timestamp).ToList();
            }
        }
    }
}

