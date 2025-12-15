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
        public DbSet<TemplateDependency> TemplateDependencies { get; set; }
        public DbSet<Compatibility> Compatibilities { get; set; }
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
            // TemplateDependencies
            // --------------------
            modelBuilder.Entity<Template>(entity =>
            {
                entity.HasIndex(t => t.Slug).IsUnique();

                entity.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<TemplateDependency>(entity =>
            {
                entity.HasOne(d => d.Template)
                    .WithMany(t => t.Dependencies)
                    .HasForeignKey(d => d.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DependencyVersion>(entity =>
            {
                entity.HasOne(v => v.Dependency)
                    .WithMany(d => d.Versions)
                    .HasForeignKey(v => v.TemplateDependencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<DependencyCommand>(entity =>
            {
                entity.HasOne(c => c.Dependency)
                    .WithMany(d => d.Commands)
                    .HasForeignKey(c => c.TemplateDependencyId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

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
