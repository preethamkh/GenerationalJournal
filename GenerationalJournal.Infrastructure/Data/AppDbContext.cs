using GenerationalJournal.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenerationalJournal.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Family> Families => Set<Family>();
    public DbSet<FamilyMember> FamilyMembers => Set<FamilyMember>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(128).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1024);
            entity.HasOne(e => e.CreatedBy)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<FamilyMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FamilyId, e.UserId }).IsUnique();
            entity.Property(e => e.Role).HasMaxLength(64).IsRequired();
            entity.Property(e => e.RelationshipDescription).HasMaxLength(256);
            entity.HasOne(e => e.Family)
                .WithMany(f => f.Members)
                .HasForeignKey(e => e.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FamilyId);
            entity.HasIndex(e => e.AuthorId);
            entity.HasIndex(e => e.EntryDate);
            entity.Property(e => e.Title).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Mood).HasMaxLength(64);
            entity.Property(e => e.Tags).HasMaxLength(1024);
            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Family)
                .WithMany()
                .HasForeignKey(e => e.FamilyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MediaItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EntryId);
            entity.HasIndex(e => e.FamilyId);
            entity.Property(e => e.FileName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.StoredFileName).HasMaxLength(512).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(128);
            entity.Property(e => e.MediaType).HasMaxLength(16);
            entity.Property(e => e.StoragePath).HasMaxLength(1024);
            entity.HasOne(e => e.Entry)
                .WithMany()
                .HasForeignKey(e => e.EntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
