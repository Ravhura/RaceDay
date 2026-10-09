using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Part 2 addition: only Run, Walk or Cycle may be stored.
        builder.ToTable("Events", t =>
            t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run','Walk','Cycle')"));

        builder.HasKey(e => e.EventId);

        builder.Property(e => e.EventName).IsRequired().HasMaxLength(150);
        builder.Property(e => e.EventDate).HasColumnType("date");
        builder.Property(e => e.Location).HasMaxLength(150);
        builder.Property(e => e.Status).IsRequired().HasMaxLength(20).IsUnicode(false)
               .HasDefaultValue(EventStatuses.Upcoming);

        // Part 2 additions (additive; no Part 1 column removed or renamed).
        builder.Property(e => e.Description).IsRequired().HasMaxLength(1000);
        builder.Property(e => e.DistanceKm).HasColumnType("decimal(5,2)");
        builder.Property(e => e.EventType).HasConversion<string>().IsRequired()
               .HasMaxLength(10).IsUnicode(false);

        // Part 1 has no cascade: an Organiser cannot be deleted while it owns Events.
        builder.HasOne(e => e.Organiser)
               .WithMany(o => o.Events)
               .HasForeignKey(e => e.OrganiserId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Events_Organisers");
    }
}