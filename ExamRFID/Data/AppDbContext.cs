using ExamRFID.Models;
using Microsoft.EntityFrameworkCore;

namespace ExamRFID.Data
{
    // The connection to the database. Each DbSet is one table.
    // Registered in Program.cs and given to every controller.
    public class AppDbContext : DbContext
    {
        // Gets the options (SQL Server + connection string) from Program.cs.
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // The 3 tables. "= default!" tells the compiler that EF Core fills them in.
        public DbSet<Customer> Customers { get; set; } = default!;

        public DbSet<Order> Orders { get; set; } = default!;

        public DbSet<OrderItem> OrderItems { get; set; } = default!;

        // Describes the tables for the database with the Fluent API: keys, lengths, indexes and relations.
        // Called by EF Core when it builds the model (also used by Add-Migration).
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Customer>(entity =>
            {
                entity.ToTable("Customers");

                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(c => c.Email)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(c => c.Phone)
                    .IsRequired(false)
                    .HasMaxLength(30);

                entity.HasIndex(c => c.Email).IsUnique();

                entity.HasIndex(c => c.Name);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.ToTable("Orders");

                entity.HasKey(o => o.Id);

                entity.Property(o => o.OrderDate)
                    .IsRequired();

                entity.HasIndex(o => o.OrderDate);

                entity.HasOne(o => o.Customer)
                    .WithMany(c => c.Orders)
                    .HasForeignKey(o => o.CustomerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");

                entity.HasKey(i => i.Id);

                entity.Property(i => i.ProductName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(i => i.Quantity)
                    .IsRequired();

                entity.Property(i => i.UnitPrice)
                    .IsRequired()
                    .HasPrecision(18, 2);

                entity.HasOne(i => i.Order)
                    .WithMany(o => o.Items)
                    .HasForeignKey(i => i.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
