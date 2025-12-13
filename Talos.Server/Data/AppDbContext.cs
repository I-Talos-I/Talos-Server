using Microsoft.EntityFrameworkCore;
using Talos.Server.Models;
using Talos.Server.Models.Entities;

namespace Talos.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

        public DbSet<User> Users { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<Package> Packages { get; set; }
        public DbSet<PackageVersion> PackageVersions { get; set; }
        public DbSet<PackageManager> PackageManagers { get; set; }
        public DbSet<TemplateDependencies> TemplateDependencies { get; set; }
        public DbSet<Compatibility> Compatibilities { get; set; }
        public DbSet<Follow> Follows { get; set; }
        public DbSet<Post> Posts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserNotificationPreference> UserNotificationPreferences { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // User
            modelBuilder.Entity<User>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Templates)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            // Follows
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowingUser)
                .WithMany(u => u.Following)
                .HasForeignKey(f => f.FollowingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowedUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(f => f.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TemplateDependency
            modelBuilder.Entity<TemplateDependencies>()
                .HasOne(td => td.Template)
                .WithMany(t => t.TemplateDependencies)
                .HasForeignKey(td => td.TemplateId);

            modelBuilder.Entity<TemplateDependencies>()
                .HasOne(td => td.Package)
                .WithMany(p => p.TemplateDependencies)
                .HasForeignKey(td => td.PackageId);

            // PackageVersion
            modelBuilder.Entity<PackageVersion>()
                .HasOne(pv => pv.Package)
                .WithMany(p => p.PackageVersions)
                .HasForeignKey(pv => pv.PackageId);

            // Compatibility
            modelBuilder.Entity<Compatibility>()
                .HasOne(c => c.SourcePackageVersion)
                .WithMany(pv => pv.SourceCompatibilities)
                .HasForeignKey(c => c.SourcePackageVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Compatibility>()
                .HasOne(c => c.TargetPackageVersion)
                .WithMany(pv => pv.TargetCompatibilities)
                .HasForeignKey(c => c.TargetPackageVersionId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // 🔔 Notification → User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔔 Notification → Tag
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.Tag)
                .WithMany(t => t.Notifications)
                .HasForeignKey(n => n.TagId)
                .OnDelete(DeleteBehavior.SetNull);

            // ⚙️ UserNotificationPreference
            modelBuilder.Entity<UserNotificationPreference>()
                .HasIndex(p => new { p.UserId, p.TagId })
                .IsUnique();
            
            modelBuilder.Entity<RefreshToken>()
                .HasIndex(rt => rt.Token)
                .IsUnique();

            modelBuilder.Entity<RefreshToken>()
                .HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
