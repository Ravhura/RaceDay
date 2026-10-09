using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EnrolmentConfiguration : IEntityTypeConfiguration<Enrolment>
{
    public void Configure(EntityTypeBuilder<Enrolment> builder)
    {
        builder.ToTable("Enrolments");
        builder.HasKey(e => e.EnrolmentId);

        builder.Property(e => e.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).IsUnicode(false)
               .HasDefaultValue(EnrolmentStatuses.Confirmed);

        builder.HasOne(e => e.Participant)
               .WithMany(p => p.Enrolments)
               .HasForeignKey(e => e.ParticipantId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Enrolments_Participants");

        // The key Part 1 integrity rule: (EventId, CategoryId) must exist together in Categories,
        // so an enrolment can never use a category that belongs to a different event.
        builder.HasOne(e => e.Category)
               .WithMany(c => c.Enrolments)
               .HasForeignKey(e => new { e.EventId, e.CategoryId })
               .HasPrincipalKey(c => new { c.EventId, c.CategoryId })
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Enrolments_EventCategory");

        // A Participant cannot enrol in the same Event + Category twice.
        builder.HasIndex(e => new { e.ParticipantId, e.EventId, e.CategoryId })
               .IsUnique()
               .HasDatabaseName("UQ_Enrolment_Once");
    }
}