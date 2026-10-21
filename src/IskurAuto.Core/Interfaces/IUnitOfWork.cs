namespace IskurAuto.Core.Interfaces;

/// <summary>
/// Değişiklikleri tek seferde ve atomik (ya hepsi ya hiçbiri) kaydeden iş birimi.
/// Repository'ler SaveChanges çağırmaz; servis bir iş akışının sonunda bunu bir kez çağırır.
/// Örn. öğrenci kaydında User + StudentProfile + ProgramParticipation tek işlemde yazılır;
/// ortada hata olursa yarım kayıt kalmaz.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Birden fazla SaveChanges çağrısını tek bir veritabanı işleminde birleştirir.
    /// Neden gerekli: "eski atamayı kapat + yenisini ekle" akışında, "aynı anda tek açık atama"
    /// kuralını koruyan benzersiz indeks yüzünden önce kapatma kaydedilmeli, sonra ekleme yapılmalıdır;
    /// ama ikisi yine de atomik kalmalıdır.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>Commit edilmeden Dispose edilirse işlem otomatik geri alınır (rollback).</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
