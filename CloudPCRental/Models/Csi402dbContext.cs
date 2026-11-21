using Microsoft.EntityFrameworkCore;

namespace CloudPCRental.Models
{
    public class Csi402dbContext : DbContext
    {
        public Csi402dbContext(DbContextOptions<Csi402dbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ความสัมพันธ์ของ Booking กับ User
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ความสัมพันธ์ของ Booking กับ Machine
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Machine)
                .WithMany()
                .HasForeignKey(b => b.MachineId)
                .OnDelete(DeleteBehavior.Restrict);

            // การแมปตารางและคอลัมน์ของ SystemLog
            modelBuilder.Entity<SystemLog>(entity =>
            {
                entity.ToTable("system_logs");
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.LogId).HasColumnName("LogId");
                entity.Property(e => e.Timestamp).HasColumnName("Timestamp");
                entity.Property(e => e.PerformedBy).HasColumnName("PerformedBy").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Category).HasColumnName("Category").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Detail).HasColumnName("Detail").HasColumnType("text").IsRequired();
                entity.Property(e => e.Severity).HasColumnName("Severity").HasMaxLength(50).IsRequired();
                entity.Property(e => e.IpAddress).HasColumnName("IpAddress").HasMaxLength(50);
            });
        }
    }
}

