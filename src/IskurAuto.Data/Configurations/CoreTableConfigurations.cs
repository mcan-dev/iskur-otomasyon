using IskurAuto.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IskurAuto.Data.Configurations;

// Ortak ilke: hiçbir ilişkide Cascade Delete kullanmıyoruz (DeleteBehavior.Restrict).
// Belge madde 9: "geçmiş kayıtlar silinmemeli". Bir bölüm/kullanıcı silinince bağlı
// geçmişin sessizce yok olması yerine veritabanı işlemi reddeder.

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("Departments");
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.Code).IsRequired().HasMaxLength(20);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class WorkUnitConfiguration : IEntityTypeConfiguration<WorkUnit>
{
    public void Configure(EntityTypeBuilder<WorkUnit> b)
    {
        b.ToTable("WorkUnits");
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(500);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("Users");
        b.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Email).IsRequired().HasMaxLength(254);
        b.Property(x => x.PhoneNumber).HasMaxLength(20);
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(500);

        // Rol metin olarak saklanır ("Student"): veritabanını okuyan biri anlar ve
        // enum sırası değişse bile eski kayıtlar bozulmaz.
        b.Property(x => x.Role).HasConversion<string>().HasMaxLength(30).IsRequired();

        // Mükerrer kullanıcıyı uygulama kontrolüne ek olarak veritabanı da engeller.
        b.HasIndex(x => x.Email).IsUnique();

        b.HasOne(x => x.Department).WithMany()
            .HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("AuditLogs");
        b.Property(x => x.UserEmail).HasMaxLength(254);
        b.Property(x => x.Action).IsRequired().HasMaxLength(100);
        b.Property(x => x.EntityName).IsRequired().HasMaxLength(100);
        b.Property(x => x.Details).HasMaxLength(1000);
        b.Property(x => x.IpAddress).HasMaxLength(45);   // IPv6 en fazla 45 karakter

        // Kullanıcıya bilerek yabancı anahtar bağlamıyoruz: denetim izi, kullanıcı kaydından bağımsız yaşar.
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.UserId);
    }
}
