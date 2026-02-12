using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace WebApplication3.Model;

public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly IConfiguration _configuration;

    public AuthDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public DbSet<AuthSession> AuthSessions { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<PasswordHistory> PasswordHistories { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = _configuration.GetConnectionString("AuthConnectionString");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AuthConnectionString is not configured.");
        }

        optionsBuilder.UseSqlServer(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.CreditCardEncrypted).HasMaxLength(2000);
            entity.Property(e => e.Gender).IsRequired();
            entity.Property(e => e.MobileNumber).HasMaxLength(20).IsRequired();
            entity.Property(e => e.DeliveryAddressEncrypted).HasMaxLength(2000);
            entity.Property(e => e.AboutMe).HasMaxLength(500);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.EmailVerified).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.EmailVerificationToken).HasMaxLength(500);
        });

        // AuthSession configuration
        builder.Entity<AuthSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionToken).HasMaxLength(128).IsRequired();
            entity.Property(e => e.DeviceInfo).HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt).IsRequired();
            
            // Index for faster lookups by session token
            entity.HasIndex(e => e.SessionToken).IsUnique();
            
            // Index for user sessions
            entity.HasIndex(e => e.UserId);
            
            // Index for active sessions
            entity.HasIndex(e => new { e.UserId, e.TerminatedAt });
            
            // Index for expiration cleanup
            entity.HasIndex(e => e.ExpiresAt);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AuditLog configuration
        builder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.EventDescription).HasMaxLength(100);
            entity.Property(e => e.EventData).HasColumnType("nvarchar(max)");
            entity.Property(e => e.IpAddress).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.SessionId).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // Index for user audit trails
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            
            // Index for event type queries
            entity.HasIndex(e => e.EventType);
            
            // Index for cleanup by date
            entity.HasIndex(e => e.CreatedAt);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // PasswordHistory configuration
        builder.Entity<PasswordHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
