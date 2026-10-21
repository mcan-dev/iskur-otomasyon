using IskurAuto.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IskurAuto.Data.Configurations;

public class AttendanceSessionConfiguration : IEntityTypeConfiguration<AttendanceSession>
{
    public void Configure(EntityTypeBuilder<AttendanceSession> b)
    {
        b.ToTable("AttendanceSessions");
        b.Property(x => x.Title).HasMaxLength(150);
        b.Property(x => x.SecretKey).IsRequired().HasMaxLength(100);
        b.Ignore(x => x.IsClosed);

        b.HasIndex(x => x.Date);

        b.HasOne(x => x.Department).WithMany()
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> b)
    {
        b.ToTable("AttendanceRecords");
        b.Property(x => x.ManualReason).HasMaxLength(500);
        b.Property(x => x.VoidReason).HasMaxLength(500);
        b.Ignore(x => x.IsVoided);

        // Aynı öğrenci aynı oturumda yalnızca bir geçerli kayda sahip olabilir.
        b.HasIndex(x => new { x.StudentProfileId, x.AttendanceSessionId }).IsUnique()
            .HasFilter("\"VoidedAt\" IS NULL")
            .HasDatabaseName("UX_Attendance_OneActivePerStudentSession");
        b.HasIndex(x => new { x.StudentProfileId, x.Date });

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AttendanceSession).WithMany()
            .HasForeignKey(x => x.AttendanceSessionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.WorkPlanDay).WithMany()
            .HasForeignKey(x => x.WorkPlanDayId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.CreatedByUser).WithMany()
            .HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.VoidedByUser).WithMany()
            .HasForeignKey(x => x.VoidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
