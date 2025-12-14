using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        
        // Apikeys For the team 
        public DbSet<ApiKey> ApiKeys { get; set; } = null!;
        public DbSet<ApiKeyAudit> ApiKeyAudits { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --------------------
            // User
            // --------------------
            modelBuilder.Entity<User>()
                .HasMany(u => u.Posts)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Templates)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            // --------------------
            // Follows
            // --------------------
            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowingUser)
                .WithMany()
                .HasForeignKey(f => f.FollowingUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Follow>()
                .HasOne(f => f.FollowedUser)
                .WithMany()
                .HasForeignKey(f => f.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // --------------------
            // TemplateDependencies
            // --------------------
            modelBuilder.Entity<TemplateDependencies>()
                .HasOne(td => td.Template)
                .WithMany(t => t.TemplateDependencies)
                .HasForeignKey(td => td.TemplateId);

            modelBuilder.Entity<TemplateDependencies>()
                .HasOne(td => td.Package)
                .WithMany(p => p.TemplateDependencies)
                .HasForeignKey(td => td.PackageId);

            // --------------------
            // Template.Tags (JSON simple)
            // --------------------
            modelBuilder.Entity<Template>()
                .Property(t => t.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v ?? new List<string>(), (JsonSerializerOptions)null),
                    v => JsonSerializer.Deserialize<List<string>>(v ?? "[]", (JsonSerializerOptions)null)
                )
                .HasColumnType("longtext"); // usa "json" si tu MySQL lo soporta

            // --------------------
            // PackageVersion
            // --------------------
            modelBuilder.Entity<PackageVersion>()
                .HasOne(pv => pv.Package)
                .WithMany(p => p.PackageVersions)
                .HasForeignKey(pv => pv.PackageId);

            // --------------------
            // Compatibility
            // --------------------
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
        }
    }
}
