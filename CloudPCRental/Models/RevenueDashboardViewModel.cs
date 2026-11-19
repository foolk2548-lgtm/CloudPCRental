namespace CloudPCRental.Models
{
    public class RevenueDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int PendingPaymentsCount { get; set; }
        public int PendingRefundsCount { get; set; }
        public int TotalTransactionsCount { get; set; }

        // ข้อมูลกราฟรายวัน (Line Chart: ย้อนหลัง 7 - 14 วัน)
        public List<string> DailyLabels { get; set; } = new();
        public List<decimal> DailyRevenues { get; set; } = new();

        // ข้อมูลกราฟรายสัปดาห์ (Line / Bar Chart)
        public List<string> WeeklyLabels { get; set; } = new();
        public List<decimal> WeeklyRevenues { get; set; } = new();

        // ข้อมูลกราฟรายเดือน (Bar Chart: ม.ค. - ธ.ค. หรือ 12 เดือนย้อนหลัง)
        public List<string> MonthlyLabels { get; set; } = new();
        public List<decimal> MonthlyRevenues { get; set; } = new();

        // รายการธุรกรรมล่าสุด
        public List<Booking> RecentTransactions { get; set; } = new();
    }
}

