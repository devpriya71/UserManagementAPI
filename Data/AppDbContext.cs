using Microsoft.EntityFrameworkCore;
using UserManagementAPI.Models;

namespace UserManagementAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).IsRequired().HasMaxLength(255);
            entity.Property(u => u.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.LastName).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Role).HasDefaultValue("User");
        });

        // Seed admin user (password: Admin@123)
        // Hash pre-computed: BCrypt.HashPassword("Admin@123")
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            FirstName = "Super",
            LastName = "Admin",
            Email = "admin@example.com",
            PasswordHash = "$2a$11$Vn7jJTAhENRKqrOJV.3t6OOsaO/YCLjNJD5cF.DK5TaVFjnb7Yd7O",
            Role = "Admin",
            IsActive = true,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
