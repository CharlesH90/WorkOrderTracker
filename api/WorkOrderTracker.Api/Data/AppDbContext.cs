using Microsoft.EntityFrameworkCore;
using WorkOrderTracker.Api.Models;

namespace WorkOrderTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Building>(e =>
        {
            e.Property(b => b.Name).HasMaxLength(100).IsRequired();
            e.Property(b => b.Address).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Technician>(e =>
        {
            e.Property(t => t.Name).HasMaxLength(100).IsRequired();
            e.Property(t => t.Email).HasMaxLength(200).IsRequired();
            e.Property(t => t.Trade).HasMaxLength(50).IsRequired();
            e.HasIndex(t => t.Email).IsUnique();
        });

        modelBuilder.Entity<WorkOrder>(e =>
        {
            e.Property(w => w.Title).HasMaxLength(150).IsRequired();
            e.Property(w => w.Description).HasMaxLength(2000);
            e.Property(w => w.Location).HasMaxLength(100);

            // Store enums as readable strings rather than ints.
            e.Property(w => w.Priority).HasConversion<string>().HasMaxLength(20);
            e.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);

            e.HasIndex(w => w.Status);

            // A building with work orders can't be deleted out from under them.
            e.HasOne(w => w.Building)
                .WithMany(b => b.WorkOrders)
                .HasForeignKey(w => w.BuildingId)
                .OnDelete(DeleteBehavior.Restrict);

            // Removing a technician just unassigns their work orders.
            e.HasOne(w => w.Technician)
                .WithMany(t => t.WorkOrders)
                .HasForeignKey(w => w.TechnicianId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        Seed(modelBuilder);
    }

    private static void Seed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Building>().HasData(
            new Building { Id = 1, Name = "Harbor View Apartments", Address = "120 Harbor St" },
            new Building { Id = 2, Name = "Elm Street Offices", Address = "45 Elm St" });

        modelBuilder.Entity<Technician>().HasData(
            new Technician { Id = 1, Name = "Sam Rivera", Email = "sam.rivera@example.com", Trade = "HVAC" },
            new Technician { Id = 2, Name = "Jordan Lee", Email = "jordan.lee@example.com", Trade = "Plumbing" });

        modelBuilder.Entity<WorkOrder>().HasData(
            new WorkOrder
            {
                Id = 1, Title = "AC not cooling", Location = "Unit 4B",
                Priority = WorkOrderPriority.High, Status = WorkOrderStatus.InProgress,
                CreatedAt = new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc),
                DueDate = new DateTime(2026, 10, 7, 0, 0, 0, DateTimeKind.Utc),
                BuildingId = 1, TechnicianId = 1
            },
            new WorkOrder
            {
                Id = 2, Title = "Leaking sink in break room", Location = "3rd floor kitchen",
                Priority = WorkOrderPriority.Medium, Status = WorkOrderStatus.Open,
                CreatedAt = new DateTime(2026, 10, 2, 14, 30, 0, DateTimeKind.Utc),
                BuildingId = 2
            });
    }
}
