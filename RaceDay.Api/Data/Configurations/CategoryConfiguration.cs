using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.HasKey(c => c.CategoryId).HasName("PK_Categories");

        // Part 1: UNIQUE (EventId, CategoryId). EF needs this as an alternate key
        // so Enrolments can point a composite foreign key at it.
        builder.HasAlternateKey(c => new { c.EventId, c.CategoryId }).HasName("UQ_Categories_Event");

        builder.Property(c => c.CategoryName).IsRequired().HasMaxLength(100);
        builder.Property(c => c.DistanceKm).HasColumnType("decimal(5,2)");
        builder.Property(c => c.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);

        builder.HasOne(c => c.Event)
               .WithMany(e => e.Categories)
               .HasForeignKey(c => c.EventId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Categories_Events");
    }
}