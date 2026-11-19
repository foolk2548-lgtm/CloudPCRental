namespace CloudPCRental.Models
{
    public class PointTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ReferenceCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Description { get; set; } = string.Empty;
        public string SubDescription { get; set; } = string.Empty;
        public int Points { get; set; }
        public int BalanceAfter { get; set; }
        public string Type { get; set; } = "receive"; // "receive" หรือ "spend"
    }

    public class UserMembershipInfo
    {
        public int UserId { get; set; }
        public string MemberNumber { get; set; } = "CPC-77491";
        public string FullName { get; set; } = "ธนพล ดีสมเกียรติ";
        public int CurrentPoints { get; set; } = 1250;
        public int ExpiringPoints { get; set; } = 0;
        public string ExpiringDate { get; set; } = $"31 ธ.ค. {DateTime.Now.Year + 543 + 1}";
        public decimal DiscountValue => CurrentPoints * 0.20m;
        public string TierName { get; set; } = "Platinum";
        public int TierLevel { get; set; } = 4;
        public double Multiplier { get; set; } = 1.5;
        public int NextTierPoints { get; set; } = 2500;
        public string NextTierName { get; set; } = "Diamond";
        public int PointsNeededForNextTier => Math.Max(0, NextTierPoints - CurrentPoints);
        public double ProgressPercentage { get; set; } = 4.0;
        public List<PointTransaction> Transactions { get; set; } = new();
    }
}
