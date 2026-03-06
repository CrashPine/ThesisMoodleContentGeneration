using Backend.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.DAL;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Test> Tests { get; set; }
    public DbSet<TextSource> TextSources { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.MoodleUserId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(u => u.MoodleUserId)
                .IsUnique();

            entity.HasMany(u => u.Tests)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Test
        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.RawGeneratedText)
                .IsRequired();

            entity.Property(t => t.MoodleXmlContent)
                .IsRequired();

            entity.Property(t => t.CreatedAt)
                .IsRequired();
        });

        // TextSource
        modelBuilder.Entity<TextSource>(entity =>
        {
            entity.HasKey(ts => ts.Id);

            entity.Property(ts => ts.FileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(ts => ts.RawText)
                .IsRequired();

            entity.HasOne(ts => ts.User)
                .WithMany(u => u.TextSources) 
                .HasForeignKey(ts => ts.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}