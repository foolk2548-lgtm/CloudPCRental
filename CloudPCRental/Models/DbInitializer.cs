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

                // 1. ตรวจสอบและเพิ่มข้อมูลผู้ใช้งานเริ่มต้น (Admin, Manager, Staff, Accountant, Thanapol)
                var defaultUsers = new List<User>
                {
                    new User { Username = "admin", Email = "admin@cloudpc.com", Password = "admin123", Role = "Admin" },
                    new User { Username = "manager", Email = "manager@cloudpc.com", Password = "manager123", Role = "Manager" },
                    new User { Username = "staff", Email = "staff@cloudpc.com", Password = "staff123", Role = "Staff" },
                    new User { Username = "accountant", Email = "account@cloudpc.com", Password = "account123", Role = "Accountant" },
                    new User { Username = "thanapol", Email = "thanapol@example.com", Password = "user123", Role = "User" },
                    new User { Username = "user1", Email = "user1@example.com", Password = "user123", Role = "User" }
                };

                foreach (var du in defaultUsers)
                {
                    if (!await context.Users.AnyAsync(u => u.Username == du.Username || u.Email == du.Email))
                    {
                        context.Users.Add(du);
                    }
                }
                await context.SaveChangesAsync();

                // 2. ตรวจสอบและเพิ่มข้อมูลเครื่อง Cloud PC ตัวอย่างตาม Wireframe
                if (!await context.Machines.AnyAsync())
                {
                    context.Machines.AddRange(
                        new Machine
                        {
                            MachineName = "เครื่องเล่นเกมสเปกแรง PRO 01",
                            CPU = "AMD Ryzen 9 7900X",
                            RAM = "32GB DDR5",
                            GPU = "RTX 4080 (16GB GDDR6X)",
                            PricePerHour = 15.00m,
                            Status = "Available"
                        },
                        new Machine
                        {
                            MachineName = "เครื่องทำงานตัดต่อ Render 02",
                            CPU = "Intel Core i9-13900K",
                            RAM = "64GB DDR5",
                            GPU = "RTX 4090 (24GB GDDR6X)",
                            PricePerHour = 25.00m,
                            Status = "Rented"
                        },
                        new Machine
                        {
                            MachineName = "เครื่องประมวลผล AI/Data 03",
                            CPU = "AMD EPYC 9354",
                            RAM = "128GB DDR5",
                            GPU = "A100 (80GB HBM2e)",
                            PricePerHour = 40.00m,
                            Status = "Available"
                        },
                        new Machine
                        {
                            MachineName = "เครื่องสเปกเริ่มต้นทั่วไป Work 04",
                            CPU = "Intel Core i5-13400",
                            RAM = "16GB DDR4",
                            GPU = "RTX 3060 (12GB GDDR6)",
                            PricePerHour = 6.00m,
                            Status = "Available"
                        },
                        new Machine
                        {
                            MachineName = "เครื่องดีไซเนอร์สเปกทำงาน 05",
                            CPU = "Intel Core i7-13700",
                            RAM = "32GB DDR5",
                            GPU = "RTX 4070 (12GB)",
                            PricePerHour = 12.00m,
                            Status = "Maintenance"
                        },
                        new Machine
                        {
                            MachineName = "เซิร์ฟเวอร์จำลองโปรแกรม Dev 06",
                            CPU = "Intel Xeon W-2245",
                            RAM = "64GB DDR4 ECC",
                            GPU = "RTX 3080 (10GB)",
                            PricePerHour = 18.00m,
                            Status = "Available"
                        }
                    );
                    await context.SaveChangesAsync();
                }

                // 3. ตรวจสอบและเพิ่มข้อมูลการจองและการทำธุรกรรมเริ่มต้น (สำหรับแสดงในระบบบัญชี)
                if (!await context.Bookings.AnyAsync())
                {
                    var users = await context.Users.ToListAsync();
                    var machines = await context.Machines.ToListAsync();

                    int userThanapolId = users.FirstOrDefault(u => u.Username.Contains("thanapol"))?.UserId ?? 1;
                    int user1Id = users.FirstOrDefault(u => u.Username.Contains("user1"))?.UserId ?? 2;
                    int m1Id = machines.FirstOrDefault()?.MachineId ?? 1;
                    int m2Id = machines.Skip(1).FirstOrDefault()?.MachineId ?? 1;
                    int m3Id = machines.Skip(2).FirstOrDefault()?.MachineId ?? 1;

                    context.Bookings.AddRange(
                        new Booking
                        {
                            UserId = userThanapolId,
                            MachineId = m1Id,
                            TotalAmount = 30.00m,
                            PaymentDate = DateTime.Now.AddDays(-1),
                            PaymentStatus = "Approved",
                            RentalHours = 2,
                            Note = "ชำระเงินและอนุมัติเรียบร้อย"
                        },
                        new Booking
                        {
                            UserId = user1Id,
                            MachineId = m2Id,
                            TotalAmount = 50.00m,
                            PaymentDate = DateTime.Now.AddDays(-2),
                            PaymentStatus = "Approved",
                            RentalHours = 2,
                            Note = "ชำระเงินและอนุมัติเรียบร้อย"
                        },
                        new Booking
                        {
                            UserId = userThanapolId,
                            MachineId = m3Id,
                            TotalAmount = 40.00m,
                            PaymentDate = DateTime.Now.AddHours(-3),
                            PaymentStatus = "Pending",
                            RentalHours = 1,
                            SlipImageUrl = "https://placehold.co/400x600/e2e8f0/1e293b?text=PromptPay+Slip+Sample"
                        },
                        new Booking
                        {
                            UserId = user1Id,
                            MachineId = m1Id,
                            TotalAmount = 15.00m,
                            PaymentDate = DateTime.Now.AddHours(-1),
                            PaymentStatus = "RefundRequested",
                            RentalHours = 1,
                            RefundReason = "เครื่องมีปัญหาการเชื่อมต่อ ขอยกเลิกและคืนเงิน"
                        }
                    );
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

