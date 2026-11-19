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
        }
    }
}

