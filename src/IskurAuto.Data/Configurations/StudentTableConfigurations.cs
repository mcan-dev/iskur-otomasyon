using IskurAuto.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IskurAuto.Data.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> b)
    {
        b.ToTable("StudentProfiles");

        b.Property(x => x.NationalId).IsRequired().HasMaxLength(11).IsFixedLength();
        b.Property(x => x.Iban).IsRequired().HasMaxLength(26);
        b.Property(x => x.StudentNumber).IsRequired().HasMaxLength(20);

        // Belge madde 9: "Mükerrer öğrenci kayıtlarını önlemek için benzersizlik kontrolleri".
        b.HasIndex(x => x.NationalId).IsUnique();
        b.HasIndex(x => x.StudentNumber).IsUnique();

        // Bir kullanıcının en fazla bir öğrenci profili olur (1-1).
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User).WithOne(u => u.StudentProfile)
            .HasForeignKey<StudentProfile>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.Department).WithMany()
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        // IsActive / Current* türetilmiş özelliklerdir (setter'ı yok), EF bunları zaten eşlemez;
        // yine de niyeti açıkça belirtiyoruz.
        b.Ignore(x => x.IsActive);
        b.Ignore(x => x.CurrentParticipation);
        b.Ignore(x => x.CurrentUnitAssignment);
        b.Ignore(x => x.CurrentAdvisorAssignment);
    }
}

public class ProgramParticipationConfiguration : IEntityTypeConfiguration<ProgramParticipation>
{
    public void Configure(EntityTypeBuilder<ProgramParticipation> b)
    {
        b.ToTable("ProgramParticipations");
        b.Property(x => x.EndReason).HasMaxLength(1000);
        b.Ignore(x => x.IsOpen);

        b.HasOne(x => x.StudentProfile).WithMany(s => s.Participations)
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);

        // Filtreli benzersiz indeks: bir öğrencinin AYNI ANDA en fazla bir açık (bitişi olmayan)
        // katılım dönemi olabilir; kapalı (geçmiş) dönemlerin sayısında sınır yoktur.
        // Uygulama kodu bunu kontrol eder, ama iki istek aynı anda gelirse son güvenceyi veritabanı verir.
        b.HasIndex(x => x.StudentProfileId).IsUnique().HasFilter("\"EndDate\" IS NULL")
            .HasDatabaseName("UX_ProgramParticipations_OneOpenPerStudent");
    }
}

public class StudentUnitAssignmentConfiguration : IEntityTypeConfiguration<StudentUnitAssignment>
{
    public void Configure(EntityTypeBuilder<StudentUnitAssignment> b)
    {
        b.ToTable("StudentUnitAssignments");
        b.Ignore(x => x.IsOpen);

        b.HasOne(x => x.StudentProfile).WithMany(s => s.UnitAssignments)
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.WorkUnit).WithMany()
            .HasForeignKey(x => x.WorkUnitId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedByUser).WithMany()
            .HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.StudentProfileId).IsUnique().HasFilter("\"EndDate\" IS NULL")
            .HasDatabaseName("UX_StudentUnitAssignments_OneOpenPerStudent");
    }
}

public class AdvisorAssignmentConfiguration : IEntityTypeConfiguration<AdvisorAssignment>
{
    public void Configure(EntityTypeBuilder<AdvisorAssignment> b)
    {
        b.ToTable("AdvisorAssignments");
        b.Ignore(x => x.IsOpen);

        b.HasOne(x => x.StudentProfile).WithMany(s => s.AdvisorAssignments)
            .HasForeignKey(x => x.StudentProfileId).OnDelete(DeleteBehavior.Restrict);

        // User tablosuna iki ayrı yabancı anahtar var (danışman ve atayan): ikisi de açıkça belirtilir.
        b.HasOne(x => x.AdvisorUser).WithMany()
            .HasForeignKey(x => x.AdvisorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedByUser).WithMany()
            .HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.StudentProfileId).IsUnique().HasFilter("\"EndDate\" IS NULL")
            .HasDatabaseName("UX_AdvisorAssignments_OneOpenPerStudent");

        // "Bu danışmanın açık öğrencileri" sorgusu için.
        b.HasIndex(x => x.AdvisorUserId);
    }
}
