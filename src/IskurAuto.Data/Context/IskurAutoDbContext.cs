using IskurAuto.Core.Entities;
using IskurAuto.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace IskurAuto.Data.Context;

/// <summary>
/// Uygulamanın EF Core veritabanı bağlamı. Aynı zamanda <see cref="IUnitOfWork"/> görevi görür:
/// servisler yalnızca Core'daki arayüzü bilir, EF Core'u bilmez.
/// </summary>
public class IskurAutoDbContext : DbContext, IUnitOfWork
{
    public IskurAutoDbContext(DbContextOptions<IskurAutoDbContext> options) : base(options) { }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<WorkUnit> WorkUnits => Set<WorkUnit>();
    public DbSet<User> Users => Set<User>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<ProgramParticipation> ProgramParticipations => Set<ProgramParticipation>();
    public DbSet<StudentUnitAssignment> StudentUnitAssignments => Set<StudentUnitAssignment>();
    public DbSet<AdvisorAssignment> AdvisorAssignments => Set<AdvisorAssignment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Holiday> Holidays => Set<Holiday>();
    public DbSet<ExamPeriod> ExamPeriods => Set<ExamPeriod>();
    public DbSet<StudentCourseDay> StudentCourseDays => Set<StudentCourseDay>();
    public DbSet<MonthlyWorkPlan> WorkPlans => Set<MonthlyWorkPlan>();
    public DbSet<WorkPlanDay> WorkPlanDays => Set<WorkPlanDay>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<Absence> Absences => Set<Absence>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AttendanceSession> AttendanceSessions => Set<AttendanceSession>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tablo ayarları ayrı IEntityTypeConfiguration sınıflarındadır (Configurations klasörü).
        // Neden: bu dosya her yeni tabloyla şişmez; her tablonun kuralı kendi dosyasında okunur.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IskurAutoDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        => new EfTransaction(await Database.BeginTransactionAsync(cancellationToken));

    /// <summary>CreatedAt / UpdatedAt alanlarını otomatik doldurur.</summary>
    private void StampTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }

    /// <summary>EF Core işlemini Core'un soyut arayüzüne saran küçük adaptör.</summary>
    private sealed class EfTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _inner;

        public EfTransaction(IDbContextTransaction inner) => _inner = inner;

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => _inner.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => _inner.DisposeAsync();
    }
}
