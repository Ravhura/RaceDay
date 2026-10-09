using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data.Configurations;

public class ResultConfiguration : IEntityTypeConfiguration<Result>
{
    public void Configure(EntityTypeBuilder<Result> builder)
    {
        builder.ToTable("Results");
        builder.HasKey(r => r.ResultId);

        builder.Property(r => r.FinishTime).HasColumnType("time");

        // Enrolment 1 -> 0..1 Result: EnrolmentId is unique.
        builder.HasOne(r => r.Enrolment)
               .WithOne(e => e.Result)
               .HasForeignKey<Result>(r => r.EnrolmentId)
               .OnDelete(DeleteBehavior.Restrict)
               .HasConstraintName("FK_Results_Enrolments");

        builder.HasIndex(r => r.EnrolmentId).IsUnique().HasDatabaseName("UQ_Results_EnrolmentId");
    }
}