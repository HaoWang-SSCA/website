using Microsoft.EntityFrameworkCore;
using SSCA.website.API.Models;

namespace SSCA.website.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<MessageMeeting> MessageMeetings { get; set; } = null!;
    public DbSet<HeroLink> HeroLinks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<MessageMeeting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.IsGospel);
            entity.HasIndex(e => e.IsSpecialMeeting);
            
            entity.Property(e => e.Date)
                .IsRequired();
            entity.Property(e => e.Speaker)
                .IsRequired()
                .HasMaxLength(100);
            entity.Property(e => e.Topic)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.AudioBlobName)
                .HasMaxLength(500);
            entity.Property(e => e.VideoUrl)
                .HasMaxLength(500);
        });

        modelBuilder.Entity<HeroLink>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExpiryDate);
            entity.HasIndex(e => e.DisplayOrder);

            entity.Property(e => e.Text)
                .IsRequired()
                .HasMaxLength(200);
            entity.Property(e => e.ExternalUrl)
                .HasMaxLength(500);
            entity.Property(e => e.FileBlobName)
                .HasMaxLength(500);
        });
    }
}
