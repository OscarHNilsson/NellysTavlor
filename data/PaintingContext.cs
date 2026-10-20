using Microsoft.EntityFrameworkCore;
using Paintings.Models;

namespace Paintings.Data;

public class PaintingContext : DbContext
{
    public PaintingContext(DbContextOptions<PaintingContext> options) : base(options) {}
    public DbSet<Painting> Paintings => Set<Painting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var painting = modelBuilder.Entity<Painting>();
        painting.ToTable("paintings", "public");
        painting.HasKey(p => p.Id);
        painting.Property(p => p.Id).HasColumnName("id").ValueGeneratedOnAdd();
        painting.Property(p => p.Title).HasColumnName("title").IsRequired();
        painting.Property(p => p.Medium).HasColumnName("medium").IsRequired();
        painting.Property(p => p.Width).HasColumnName("width").HasPrecision(8, 2);
        painting.Property(p => p.Height).HasColumnName("height").HasPrecision(8, 2);
        painting.Property(p => p.ImagePath).HasColumnName("image_path").IsRequired();
        painting.Property(p => p.UploadedAt).HasColumnName("uploaded_at");
    }
}
