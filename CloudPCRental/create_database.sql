-- ===============================================================
-- CloudPC Rental Database Initialization Script
-- ฐานข้อมูล: csi402db
-- ===============================================================

CREATE DATABASE IF NOT EXISTS `csi402db` 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE `csi402db`;

-- 1. ตาราง users (ผู้ใช้งานและบทบาท)
CREATE TABLE IF NOT EXISTS `users` (
    `user_id` INT AUTO_INCREMENT PRIMARY KEY,
    `username` VARCHAR(50) NOT NULL UNIQUE,
    `email` VARCHAR(100) NOT NULL UNIQUE,
    `password` VARCHAR(255) NOT NULL,
    `role` VARCHAR(50) NOT NULL DEFAULT 'User',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 2. ตาราง machines (เครื่องคอมพิวเตอร์ Cloud PC)
CREATE TABLE IF NOT EXISTS `machines` (
    `MachineId` INT AUTO_INCREMENT PRIMARY KEY,
    `MachineName` VARCHAR(100) NOT NULL,
    `CPU` VARCHAR(100) NOT NULL,
    `RAM` VARCHAR(50) NOT NULL,
    `GPU` VARCHAR(100) NOT NULL,
    `PricePerHour` DECIMAL(10,2) NOT NULL,
    `Status` VARCHAR(50) NOT NULL DEFAULT 'Available'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

-- 3. ตาราง bookings (รายการจองและการตรวจสลิปโอนเงิน)
CREATE TABLE IF NOT EXISTS `bookings` (
    `BookingId` INT AUTO_INCREMENT PRIMARY KEY,
    `UserId` INT NOT NULL,
    `MachineId` INT NOT NULL,
    `TotalAmount` DECIMAL(10,2) NOT NULL,
    `PaymentDate` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `PaymentStatus` VARCHAR(50) NOT NULL DEFAULT 'Approved',
    `SlipImageUrl` VARCHAR(255) NULL,
    `RefundSlipImageUrl` VARCHAR(255) NULL,
    `RefundReason` VARCHAR(255) NULL,
    `RefundDate` DATETIME NULL,
    `Note` VARCHAR(255) NULL,
    `RentalHours` INT NOT NULL DEFAULT 1,
    `StartTime` DATETIME NULL,
    `EndTime` DATETIME NULL,
    CONSTRAINT `fk_bookings_user` FOREIGN KEY (`UserId`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
    CONSTRAINT `fk_bookings_machine` FOREIGN KEY (`MachineId`) REFERENCES `machines` (`MachineId`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=95000 DEFAULT CHARSET=utf8mb4;

-- ===============================================================
-- ข้อมูลเริ่มต้น (Seed Data)
-- ===============================================================

-- บัญชีผู้ใช้งานระบบครบทั้ง 5 บทบาท + บัญชีของคุณ
INSERT IGNORE INTO `users` (`user_id`, `username`, `email`, `password`, `role`) VALUES
(1, 'admin', 'admin@cloudpc.com', 'admin123', 'Admin'),
(2, 'manager', 'manager@cloudpc.com', 'manager123', 'Manager'),
(3, 'staff', 'staff@cloudpc.com', 'staff123', 'Staff'),
(4, 'accountant', 'account@cloudpc.com', 'account123', 'Accountant'),
(5, 'thanapol', 'thanapol@example.com', 'user123', 'User'),
(6, 'foolk2548@gmail.com', 'foolk2548@gmail.com', '123456', 'User');

-- รายการเครื่อง Cloud PC ตัวอย่างตาม Wireframe
INSERT IGNORE INTO `machines` (`MachineId`, `MachineName`, `CPU`, `RAM`, `GPU`, `PricePerHour`, `Status`) VALUES
(1, 'เครื่องเล่นเกมสเปกแรง PRO 01', 'AMD Ryzen 9 7900X', '32GB DDR5', 'RTX 4080 (16GB GDDR6X)', 15.00, 'Available'),
(2, 'เครื่องทำงานตัดต่อ Render 02', 'Intel Core i9-13900K', '64GB DDR5', 'RTX 4090 (24GB GDDR6X)', 25.00, 'Rented'),
(3, 'เครื่องประมวลผล AI/Data 03', 'AMD EPYC 9354', '128GB DDR5', 'A100 (80GB HBM2e)', 40.00, 'Available'),
(4, 'เครื่องสเปกเริ่มต้นทั่วไป Work 04', 'Intel Core i5-13400', '16GB DDR4', 'RTX 3060 (12GB GDDR6)', 6.00, 'Available'),
(5, 'เครื่องดีไซเนอร์สเปกทำงาน 05', 'Intel Core i7-13700', '32GB DDR5', 'RTX 4070 (12GB)', 12.00, 'Maintenance'),
(6, 'เซิร์ฟเวอร์จำลองโปรแกรม Dev 06', 'Intel Xeon W-2245', '64GB DDR4 ECC', 'RTX 3080 (10GB)', 18.00, 'Available');

-- รายการจองตัวอย่างสำหรับแสดงในประวัติการจองและระบบบัญชี
INSERT IGNORE INTO `bookings` (`BookingId`, `UserId`, `MachineId`, `TotalAmount`, `PaymentDate`, `PaymentStatus`, `RentalHours`, `Note`) VALUES
(94812, 5, 1, 60.00, NOW() - INTERVAL 1 DAY, 'Refunded', 4, 'ตรวจสอบเงื่อนไขตรงตามนโยบาย: เครื่องขัดข้องจริง อนุมัติคืนเงินเต็มจำนวนเรียบร้อย (+60.00 ฿ เข้ากระเป๋าเงิน)');
