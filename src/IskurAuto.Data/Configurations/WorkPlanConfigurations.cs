using IskurAuto.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IskurAuto.Data.Configurations;

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> b)
    {
        b.ToTable("Holidays");
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.HasIndex(x => x.Date).IsUnique();
    }
}

public class ExamPeriodConfiguration : IEntityTypeConfiguration<ExamPeriod>
{
    public void Configure(EntityTypeBuilder<ExamPeriod> b)
    {
        b.ToTable("ExamPeriods");
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
    }
}

public class StudentCourseDayConfiguration : IEntityTypeConfiguration<StudentCourseDay>
{
    public void Configure(EntityTypeBuilder<StudentCourseDay> b)
    {
        b.ToTable("StudentCourseDays");
        b.HasIndex(x => new { x.StudentProfileId, x.DayOfWeek }).IsUnique();
        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MonthlyWorkPlanConfiguration : IEntityTypeConfiguration<MonthlyWorkPlan>
{
    public void Configure(EntityTypeBuilder<MonthlyWorkPlan> b)
    {
        b.ToTable("MonthlyWorkPlans");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.StudentNote).HasMaxLength(1000);
        b.Property(x => x.ReviewNote).HasMaxLength(1000);

        b.HasIndex(x => new { x.StudentProfileId, x.Year, x.Month }).IsUnique();

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReviewedByUser).WithMany()
            .HasForeignKey(x => x.ReviewedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkPlanDayConfiguration : IEntityTypeConfiguration<WorkPlanDay>
{
    public void Configure(EntityTypeBuilder<WorkPlanDay> b)
    {
        b.ToTable("WorkPlanDays");
        b.Property(x => x.Note).HasMaxLength(500);

        b.HasIndex(x => new { x.MonthlyWorkPlanId, x.Date }).IsUnique();

        b.HasOne(x => x.MonthlyWorkPlan).WithMany(p => p.Days)
            .HasForeignKey(x => x.MonthlyWorkPlanId).OnDelete(DeleteBehavior.Restrict);
    }
}
