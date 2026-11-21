namespace CloudPCRental.Models
{
    /// <summary>
    /// ข้อมูลสรุปสถิติการใช้งานเครื่องตามช่วงเวลาในแต่ละวัน
    /// </summary>
    public class TimeSlotUsageItem
    {
        public string TimeRange { get; set; } = string.Empty;
        public int MachineCount { get; set; }
        public int Percentage { get; set; }
    }

    /// <summary>
    /// ข้อมูลสเปกคอมพิวเตอร์ที่ได้รับความนิยมและถูกจองมากที่สุด
    /// </summary>
    public class PopularSpecItem
    {
        public int Rank { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int BookingCount { get; set; }
    }

    /// <summary>
    /// โมเดลรวมสถิติการใช้งานสำหรับหน้าแดชบอร์ดหลักผู้ดูแลระบบ
    /// </summary>
    public class DashboardUsageStats
    {
        public List<TimeSlotUsageItem> TimeSlots { get; set; } = new();
        public List<PopularSpecItem> PopularSpecs { get; set; } = new();
    }
}
