using Microsoft.EntityFrameworkCore;

namespace CloudPCRental.Models
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(Csi402dbContext context)
        {
            try
            {
                bool canConnect = await context.Database.CanConnectAsync();
                if (!canConnect)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("\n[CloudPC DB] [!] ไม่สามารถเชื่อมต่อ MySQL Server ได้ (แอปพลิเคชันจะทำงานในโหมด MockDataStore)");
                    Console.ResetColor();
                    return;
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("\n=======================================================");
                Console.WriteLine("[CloudPC DB] [SUCCESS] เชื่อมต่อ MySQL Database (csi402db) สำเร็จ!");
                Console.WriteLine("=======================================================");
                Console.ResetColor();

                // ตรวจสอบและสร้างตารางหากยังไม่มีใน MySQL Database
                await context.Database.EnsureCreatedAsync();

                // 1. ตรวจสอบและซิงค์ข้อมูลผู้ใช้งานให้สอดคล้องกับระบบจริง (MockDataStore)
                var mockUsers = MockDataStore.GetUsers();
                var dbUsers = await context.Users.ToListAsync();

                // ลบบัญชีผู้ใช้ที่ถูกแอดมินลบออกไปแล้วในระบบ (เช่น thanapol, user1) เพื่อความสอดคล้อง 100%
                var mockUsernames = mockUsers.Select(u => u.Username.ToLower()).ToHashSet();
                var obsoleteUsers = dbUsers.Where(u => !mockUsernames.Contains(u.Username.ToLower())).ToList();
                if (obsoleteUsers.Any())
                {
                    context.Users.RemoveRange(obsoleteUsers);
                    await context.SaveChangesAsync();
                    dbUsers = await context.Users.ToListAsync();
                }

                // ซิงค์บัญชีผู้ใช้ปัจจุบันเข้า Database หากยังไม่มี
                foreach (var mu in mockUsers)
                {
                    if (!dbUsers.Any(u => u.Username.Equals(mu.Username, StringComparison.OrdinalIgnoreCase) || u.Email.Equals(mu.Email, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Users.Add(new User
                        {
                            Username = mu.Username,
                            Email = mu.Email,
                            Password = mu.Password,
                            Role = mu.Role
                        });
                    }
                }
                await context.SaveChangesAsync();

                // 2. ตรวจสอบและซิงค์ข้อมูลเครื่อง Cloud PC ให้ครบทั้ง 22 เครื่องตรงตาม Wireframe หน้า 3
                var mockMachines = MockDataStore.GetMachines();
                var dbMachines = await context.Machines.ToListAsync();

                foreach (var mm in mockMachines)
                {
                    var exist = dbMachines.FirstOrDefault(m => m.MachineId == mm.MachineId || m.MachineName.Equals(mm.MachineName, StringComparison.OrdinalIgnoreCase));
                    if (exist == null)
                    {
                        context.Machines.Add(new Machine
                        {
                            MachineName = mm.MachineName,
                            CPU = mm.CPU,
                            RAM = mm.RAM,
                            GPU = mm.GPU,
                            PricePerHour = mm.PricePerHour,
                            Status = mm.Status
                        });
                    }
                    else
                    {
                        exist.MachineName = mm.MachineName;
                        exist.CPU = mm.CPU;
                        exist.RAM = mm.RAM;
                        exist.GPU = mm.GPU;
                        exist.PricePerHour = mm.PricePerHour;
                    }
                }
                await context.SaveChangesAsync();

                // 3. ทำความสะอาดข้อมูลประวัติการจอง: ลบรายการจำลอง/ยอด 0 บาท/ภาพ Unsplash และกำจัดรายการซ้ำซ้อน
                var dummyBookings = await context.Bookings.Where(b => 
                    b.TotalAmount <= 0 ||
                    (b.BookingId >= 8000 && b.BookingId <= 8025) ||
                    (b.BookingId >= 100 && b.BookingId <= 115) ||
                    b.BookingId == 94812 ||
                    (b.SlipImageUrl != null && b.SlipImageUrl.Contains("unsplash")) ||
                    (b.Note != null && b.Note.Contains("ยอดสะสมค่าเช่าคลาวด์คอมพิวเตอร์")) ||
                    (b.Note != null && b.Note.Contains("K-Bank ยอด 60")) ||
                    (b.Note != null && b.Note.Contains("SCB พร้อมเพย์"))
                ).ToListAsync();
                if (dummyBookings.Any())
                {
                    context.Bookings.RemoveRange(dummyBookings);
                    await context.SaveChangesAsync();
                }

                var allDbBookings = await context.Bookings.OrderBy(b => b.BookingId).ToListAsync();
                var seenKeys = new HashSet<string>();
                var dups = new List<Booking>();
                foreach (var b in allDbBookings)
                {
                    string key;
                    if (!string.IsNullOrEmpty(b.SlipImageUrl))
                    {
                        key = "slip:" + b.SlipImageUrl.Trim();
                    }
                    else
                    {
                        key = $"txn:{b.MachineId}_{b.TotalAmount:F2}_{b.PaymentDate:yyyyMMddHHmm}_{b.PaymentStatus}";
                    }

                    if (seenKeys.Contains(key))
                    {
                        dups.Add(b);
                    }
                    else
                    {
                        seenKeys.Add(key);
                    }
                }
                if (dups.Any())
                {
                    context.Bookings.RemoveRange(dups);
                    await context.SaveChangesAsync();
                }

                int userCount = await context.Users.CountAsync();
                int machineCount = await context.Machines.CountAsync();
                int bookingCount = await context.Bookings.CountAsync();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"[CloudPC DB] [DATA] พร้อมใช้งาน: ผู้ใช้ {userCount} บัญชี, เครื่อง {machineCount} เครื่อง, ประวัติการจอง {bookingCount} รายการ");
                Console.WriteLine("=======================================================\n");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n=======================================================");
                Console.WriteLine($"[CloudPC DB] [ERROR] เกิดข้อผิดพลาดในการเชื่อมต่อ Database: {ex.Message}");
                Console.WriteLine("[CloudPC DB] [INFO] แอปพลิเคชันจะทำงานต่อด้วย MockDataStore เพื่อความต่อเนื่องของระบบ");
                Console.WriteLine("=======================================================\n");
                Console.ResetColor();
            }
        }
    }
}

