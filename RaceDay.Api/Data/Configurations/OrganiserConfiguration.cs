using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class OrganiserConfiguration : IEntityTypeConfiguration<Organiser>
{
    public void Configure(EntityTypeBuilder<Organiser> builder)
    {
        builder.ToTable("Organisers");
        builder.HasKey(o => o.OrganiserId);

        builder.Property(o => o.FullName).IsRequired().HasMaxLength(100);
        builder.Property(o => o.Email).IsRequired().HasMaxLength(150);
        builder.Property(o => o.PasswordHash).IsRequired().HasMaxLength(255);
        builder.Property(o => o.PhoneNumber).HasMaxLength(20).IsUnicode(false);
        builder.Property(o => o.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

        // Part 1: Email UNIQUE (enforced as a unique index; behaviour is identical).
        builder.HasIndex(o => o.Email).IsUnique().HasDatabaseName("UQ_Organisers_Email");
    }
}