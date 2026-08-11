using Microsoft.EntityFrameworkCore;
using SRM.Api.Models.Entities;

namespace SRM.Api.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<AppUser> AppUsers { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            /* index hace la query mas rapida, se filtran los que estan eliminados */

            // apartment
            modelBuilder.Entity<Apartment>().HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<Apartment>()
                .HasIndex(r => r.IsDeleted)
                .HasFilter("IsDeleted = 0");

            modelBuilder.Entity<Apartment>()
                .HasMany(a => a.Images)
                .WithOne(i => i.Apartment)
                .HasForeignKey(a => a.ApartamentId)
                .IsRequired();

            modelBuilder.Entity<Apartment>()
                .HasMany(a => a.Reservations)
                .WithOne(r => r.Apartment)
                .HasForeignKey(r => r.ApartmentId)
                .IsRequired();



            // user
            modelBuilder.Entity<AppUser>().HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<AppUser>()
                .HasIndex(r => r.IsDeleted)
                .HasFilter("IsDeleted = 0");

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Reservations)
                .WithOne(r => r.AppUser)
                .HasForeignKey(r => r.AppUserId)
                .IsRequired();

            modelBuilder.Entity<AppUser>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.AppUser)
                .HasForeignKey(p => p.AppUserId)
                .IsRequired();



            // reservation
            modelBuilder.Entity<Reservation>().HasQueryFilter(r => !r.IsDeleted);

            modelBuilder.Entity<Reservation>()
                .HasIndex(r => r.IsDeleted)
                .HasFilter("IsDeleted = 0");

            modelBuilder.Entity<Reservation>()
                .HasMany(r => r.Payments)
                .WithOne(p => p.Reservation)
                .HasForeignKey(p => p.ReservationId)
                .IsRequired();



            // payment
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Ticket)
                .WithOne(t => t.Payment)
                .HasForeignKey<Payment>(p => p.TicketId)
                .IsRequired(false);
        }
    }
}