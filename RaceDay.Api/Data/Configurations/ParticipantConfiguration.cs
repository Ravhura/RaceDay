using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("Participants");
        builder.HasKey(p => p.ParticipantId);

        builder.Property(p => p.FullName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Email).IsRequired().HasMaxLength(150);
        builder.Property(p => p.PasswordHash).IsRequired().HasMaxLength(255);
        builder.Property(p => p.PhoneNumber).HasMaxLength(20).IsUnicode(false);
        builder.Property(p => p.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

        builder.HasIndex(p => p.Email).IsUnique().HasDatabaseName("UQ_Participants_Email");
    }
}