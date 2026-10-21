namespace IskurAuto.Core.Exceptions;

/// <summary>
/// İş kuralı ihlallerinin taban sınıfı. Servis katmanı HTTP'yi bilmez;
/// sadece "ne yanlış gitti" bilgisini bu tipli istisnalarla bildirir.
/// API katmanındaki tek bir middleware bunları HTTP durum kodlarına çevirir.
/// Neden: controller'larda try/catch tekrarını bitirir, her endpoint aynı hata biçimini döner.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>Girdi geçersiz → HTTP 400. Alan bazlı hata listesi taşıyabilir.</summary>
public class DomainValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public DomainValidationException(string message) : base(message)
        => Errors = new Dictionary<string, string[]>();

    public DomainValidationException(string field, string message) : base(message)
        => Errors = new Dictionary<string, string[]> { [field] = new[] { message } };

    public DomainValidationException(IDictionary<string, string[]> errors)
        : base("Bir veya daha fazla doğrulama hatası oluştu.")
        => Errors = new Dictionary<string, string[]>(errors);
}

/// <summary>Kayıt bulunamadı → HTTP 404.</summary>
public class NotFoundException : DomainException
{
    public NotFoundException(string message) : base(message) { }
}

/// <summary>Mevcut verilerle çakışma (örn. mükerrer TC) → HTTP 409.</summary>
public class ConflictException : DomainException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Kimliği belli ama yetkisi yok → HTTP 403.</summary>
public class ForbiddenException : DomainException
{
    public ForbiddenException(string message = "Bu işlem için yetkiniz bulunmuyor.") : base(message) { }
}

/// <summary>Kimlik doğrulama başarısız (yanlış parola vb.) → HTTP 401.</summary>
public class AuthenticationFailedException : DomainException
{
    public AuthenticationFailedException(string message = "E-posta veya parola hatalı.") : base(message) { }
}
