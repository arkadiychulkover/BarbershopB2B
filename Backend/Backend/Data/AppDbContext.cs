using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<BarbershopOwner> BarbershopOwners { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Master> Masters { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<ServiceName> ServiceNames { get; set; }
        public DbSet<Shift> Shifts { get; set; }
        public DbSet<Tranzaction> Tranxactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Master>()
                .HasOne(m => m.Owner)
                .WithMany(o => o.Masters)
                .HasForeignKey(m => m.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Client>()
                .HasOne(c => c.Owner)
                .WithMany(o => o.Clients)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.Master)
                .WithMany(m => m.Services)
                .HasForeignKey(s => s.MasterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Shift>()
                .HasOne(s => s.Master)
                .WithMany(m => m.Shifts)
                .HasForeignKey(s => s.MasterId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Master)
                .WithMany(m => m.Appointments)
                .HasForeignKey(a => a.MasterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Client)
                .WithMany(c => c.Appointments)
                .HasForeignKey(a => a.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Service)
                .WithMany()
                .HasForeignKey(a => a.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Appointment)
                .WithMany()
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Master)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MasterId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Client)
                .WithMany()
                .HasForeignKey(r => r.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Service>()
                .HasOne(s => s.ServiceName)
                .WithMany()
                .HasForeignKey(s => s.ServiceNameId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ServiceName>()
                .HasOne(sn => sn.Owner)
                .WithMany()
                .HasForeignKey(sn => sn.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
