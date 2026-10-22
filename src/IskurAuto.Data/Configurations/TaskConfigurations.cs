using IskurAuto.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IskurAuto.Data.Configurations;

public class StudentTaskConfiguration : IEntityTypeConfiguration<StudentTask>
{
    public void Configure(EntityTypeBuilder<StudentTask> b)
    {
        b.ToTable("StudentTasks");
        b.Property(x => x.Title).IsRequired().HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => new { x.StudentProfileId, x.Status });

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedByUser).WithMany()
            .HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Evaluations).WithOne(e => e.StudentTask)
            .HasForeignKey(e => e.StudentTaskId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TaskEvaluationConfiguration : IEntityTypeConfiguration<TaskEvaluation>
{
    public void Configure(EntityTypeBuilder<TaskEvaluation> b)
    {
        b.ToTable("TaskEvaluations");
        b.Property(x => x.Note).HasMaxLength(2000);
        b.Property(x => x.NewStatus).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.AuthorUser).WithMany()
            .HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AbsenceNoticeConfiguration : IEntityTypeConfiguration<AbsenceNotice>
{
    public void Configure(EntityTypeBuilder<AbsenceNotice> b)
    {
        b.ToTable("AbsenceNotices");
        b.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        b.Ignore(x => x.IsRead);

        // Aynı danışman aynı öğrenci ve gün için tek bildirim gönderebilir.
        b.HasIndex(x => new { x.AdvisorUserId, x.StudentProfileId, x.Date }).IsUnique();

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AdvisorUser).WithMany()
            .HasForeignKey(x => x.AdvisorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReadByUser).WithMany()
            .HasForeignKey(x => x.ReadByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class TerminationDecisionConfiguration : IEntityTypeConfiguration<TerminationDecision>
{
    public void Configure(EntityTypeBuilder<TerminationDecision> b)
    {
        b.ToTable("TerminationDecisions");
        b.Property(x => x.Basis).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.Note).IsRequired().HasMaxLength(1000);
        b.Property(x => x.LeaveDaysUsed).HasPrecision(5, 2);

        b.HasIndex(x => new { x.StudentProfileId, x.DecisionDate });

        b.HasOne(x => x.StudentProfile).WithMany()
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DecidedByUser).WithMany()
            .HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
