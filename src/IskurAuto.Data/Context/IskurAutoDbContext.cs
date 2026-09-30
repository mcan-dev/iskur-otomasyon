using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace IskurAuto.Data.Context;

/// <summary>
/// Uygulamanın ana veritabanı bağlam sınıfı.
/// Tüm entity'leri DbSet olarak barındırır ve Fluent API ile
/// tablo yapılandırmalarını, ilişkileri ve kısıtlamaları tanımlar.
/// </summary>
public class IskurAutoDbContext : DbContext
{
    public IskurAutoDbContext(DbContextOptions<IskurAutoDbContext> options)
        : base(options)
    {
    }

    // ─── DbSet'ler ───────────────────────────────────────────────────────────

    public DbSet<Faculty> Faculties { get; set; } = null!;
    public DbSet<Role> Roles { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<StudentWorker> StudentWorkers { get; set; } = null!;
    public DbSet<MonthlyTimesheet> MonthlyTimesheets { get; set; } = null!;
    public DbSet<TaskLog> TaskLogs { get; set; } = null!;

    // ─── Model Yapılandırması ─────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureFaculty(modelBuilder);
        ConfigureRole(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigureStudentWorker(modelBuilder);
        ConfigureMonthlyTimesheet(modelBuilder);
        ConfigureTaskLog(modelBuilder);
    }

    // ─── Yapılandırma Metodları ───────────────────────────────────────────────

    /// <summary>
    /// Multi-Tenant yapısının köküdür. Her fakülte bir kiracıya (tenant) karşılık gelir.
    /// </summary>
    private static void ConfigureFaculty(ModelBuilder mb)
    {
        mb.Entity<Faculty>(entity =>
        {
            entity.ToTable("Faculties");
            entity.HasKey(f => f.Id);

            entity.Property(f => f.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(f => f.Code)
                .IsRequired()
                .HasMaxLength(20);

            // Code alanı benzersiz olmalıdır (Örn: "MF" iki kez olamaz)
            entity.HasIndex(f => f.Code)
                .IsUnique()
                .HasDatabaseName("IX_Faculties_Code");

            entity.Property(f => f.IsActive)
                .HasDefaultValue(true);

            entity.Property(f => f.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });
    }

    private static void ConfigureRole(ModelBuilder mb)
    {
        mb.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(r => r.Name)
                .IsUnique()
                .HasDatabaseName("IX_Roles_Name");

            entity.Property(r => r.Description)
                .HasMaxLength(500);
        });
    }

    /// <summary>
    /// Kullanıcı → Rol (Çok-Bir) ve Kullanıcı → Fakülte (Çok-Bir, opsiyonel) ilişkileri.
    /// FacultyId nullable'dır; SKS Admin tüm fakülteleri yönetebilir.
    /// </summary>
    private static void ConfigureUser(ModelBuilder mb)
    {
        mb.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(u => u.Id);

            entity.Property(u => u.FullName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");

            entity.Property(u => u.PasswordHash)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(u => u.IsActive)
                .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // User → Role: Çok-Bir (Zorunlu)
            entity.HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Faculty: Çok-Bir (Opsiyonel — SKS Admin için null olabilir)
            entity.HasOne(u => u.Faculty)
                .WithMany(f => f.Users)
                .HasForeignKey(u => u.FacultyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Multi-Tenant izolasyonunun temel ilişkisi: StudentWorker → Faculty (Çok-Bir, Zorunlu).
    /// Bir öğrenci yalnızca bir fakülteye ait olabilir.
    /// </summary>
    private static void ConfigureStudentWorker(ModelBuilder mb)
    {
        mb.Entity<StudentWorker>(entity =>
        {
            entity.ToTable("StudentWorkers");
            entity.HasKey(sw => sw.Id);

            entity.Property(sw => sw.NationalId)
                .IsRequired()
                .HasMaxLength(11)
                .IsFixedLength(); // CHAR(11) — TC Kimlik No sabit 11 hane

            entity.Property(sw => sw.StudentNumber)
                .IsRequired()
                .HasMaxLength(15);

            // Öğrenci numarası sistem genelinde benzersiz olmalıdır
            entity.HasIndex(sw => sw.StudentNumber)
                .IsUnique()
                .HasDatabaseName("IX_StudentWorkers_StudentNumber");

            // Aynı öğrenci aynı fakültede iki kez kayıtlı olamaz
            entity.HasIndex(sw => new { sw.NationalId, sw.FacultyId })
                .IsUnique()
                .HasDatabaseName("IX_StudentWorkers_NationalId_FacultyId");

            entity.Property(sw => sw.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(sw => sw.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(sw => sw.IskurRegistrationNumber)
                .HasMaxLength(50);

            entity.Property(sw => sw.IsActive)
                .HasDefaultValue(true);

            entity.Property(sw => sw.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // StudentWorker → Faculty: Çok-Bir (Zorunlu — Multi-Tenant ilişkisi)
            entity.HasOne(sw => sw.Faculty)
                .WithMany(f => f.StudentWorkers)
                .HasForeignKey(sw => sw.FacultyId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    /// <summary>
    /// Maker-Checker iş akışının merkezi tablosu.
    /// MonthlyTimesheet → StudentWorker (Çok-Bir, Zorunlu)
    /// MonthlyTimesheet → User/ApprovedByUser (Çok-Bir, Opsiyonel — sadece onay sonrası dolar)
    /// </summary>
    private static void ConfigureMonthlyTimesheet(ModelBuilder mb)
    {
        mb.Entity<MonthlyTimesheet>(entity =>
        {
            entity.ToTable("MonthlyTimesheets");
            entity.HasKey(mt => mt.Id);

            // Aynı öğrencinin aynı ay/yıl için tek bir puantajı olabilir
            entity.HasIndex(mt => new { mt.StudentWorkerId, mt.Year, mt.Month })
                .IsUnique()
                .HasDatabaseName("IX_MonthlyTimesheets_Student_Year_Month");

            entity.Property(mt => mt.TotalHoursWorked)
                .HasColumnType("decimal(5,2)"); // Örn: 999.99 saat

            // Enum'u veritabanında string olarak sakla — okunabilirlik için
            entity.Property(mt => mt.Status)
                .HasConversion<string>()
                .HasMaxLength(50)
                .HasDefaultValue(TimesheetStatus.PendingSksApproval);

            entity.Property(mt => mt.MakerNote)
                .HasMaxLength(1000);

            entity.Property(mt => mt.CheckerNote)
                .HasMaxLength(1000);

            entity.Property(mt => mt.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // MonthlyTimesheet → StudentWorker: Çok-Bir (Zorunlu)
            entity.HasOne(mt => mt.StudentWorker)
                .WithMany(sw => sw.MonthlyTimesheets)
                .HasForeignKey(mt => mt.StudentWorkerId)
                .OnDelete(DeleteBehavior.Cascade);

            // MonthlyTimesheet → User (Onaylayan): Çok-Bir (Opsiyonel)
            entity.HasOne(mt => mt.ApprovedByUser)
                .WithMany()
                .HasForeignKey(mt => mt.ApprovedByUserId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    /// <summary>
    /// Playwright otomasyon botunun adım logları.
    /// TaskLog → MonthlyTimesheet (Çok-Bir, Zorunlu)
    /// </summary>
    private static void ConfigureTaskLog(ModelBuilder mb)
    {
        mb.Entity<TaskLog>(entity =>
        {
            entity.ToTable("TaskLogs");
            entity.HasKey(tl => tl.Id);

            entity.Property(tl => tl.StepDescription)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(tl => tl.ErrorDetail)
                .HasMaxLength(2000);

            // StackTrace için sınır koymuyoruz — tam hata çıktısı saklanmalı
            entity.Property(tl => tl.StackTrace)
                .HasColumnType("nvarchar(max)");

            entity.Property(tl => tl.LoggedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // TaskLog → MonthlyTimesheet: Çok-Bir (Zorunlu)
            entity.HasOne(tl => tl.MonthlyTimesheet)
                .WithMany(mt => mt.TaskLogs)
                .HasForeignKey(tl => tl.MonthlyTimesheetId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
