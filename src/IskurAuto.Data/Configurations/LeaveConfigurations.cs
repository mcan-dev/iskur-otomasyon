using IskurAuto.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IskurAuto.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> b)
    {
        b.ToTable("LeaveRequests");
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ReviewNote).HasMaxLength(1000);
        b.Property(x => x.DocumentFileName).HasMaxLength(255);
        b.Property(x => x.DocumentStoredName).HasMaxLength(100);
        b.Property(x => x.DocumentContentType).HasMaxLength(100);
        b.Property(x => x.Hours).HasPrecision(5, 2);
        b.Property(x => x.DaysCounted).HasPrecision(5, 2);
        b.Ignore(x => x.HasDocument);

        b.HasIndex(x => new { x.StudentProfileId, x.StartDate });

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReviewedByUser).WithMany()
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AbsenceConfiguration : IEntityTypeConfiguration<Absence>
{
    public void Configure(EntityTypeBuilder<Absence> b)
    {
        b.ToTable("Absences");
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Note).HasMaxLength(500);
        b.Property(x => x.RevokeReason).HasMaxLength(500);
        b.Ignore(x => x.IsRevoked);

        b.HasIndex(x => new { x.StudentProfileId, x.Date }).IsUnique().HasFilter("[RevokedAt] IS NULL")
            .HasDatabaseName("UX_Absences_OneActivePerStudentDay");

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RecordedByUser).WithMany()
            .HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RevokedByUser).WithMany()
            .HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> b)
    {
        b.ToTable("SystemSettings");
        b.Property(x => x.Key).IsRequired().HasMaxLength(100);
        b.Property(x => x.Value).IsRequired().HasMaxLength(500);
        b.HasIndex(x => x.Key).IsUnique();
    }
}
