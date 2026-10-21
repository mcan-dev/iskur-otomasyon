using IskurAuto.Core.Constants;
using IskurAuto.Core.Entities;
using IskurAuto.Core.Enums;
using IskurAuto.Core.Exceptions;
using IskurAuto.Core.Interfaces;
using IskurAuto.Core.Models;
using IskurAuto.Service.Abstractions;
using IskurAuto.Service.Common;
using IskurAuto.Service.DTOs;
using IskurAuto.Service.Interfaces;
using IskurAuto.Service.Mapping;
using IskurAuto.Service.Validation;

namespace IskurAuto.Service.Implementations;

/// <summary>
/// Öğrenci kayıtlarının görüntülenmesi ve yönetimi. Rol kapsamı (kim hangi öğrenciyi görür)
/// bu sınıfta zorlanır; controller yalnızca "giriş yapmış mı / hangi rol" gibi kaba kontrol yapar.
/// </summary>
public class StudentService : IStudentService
{
    private readonly IStudentRepository _students;
    private readonly IUserRepository _users;
    private readonly IWorkUnitRepository _units;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public StudentService(
        IStudentRepository students, IUserRepository users, IWorkUnitRepository units,
        IUnitOfWork uow, IClock clock, IAuditService audit, ICurrentUser current)
    {
        _students = students;
        _users = users;
        _units = units;
        _uow = uow;
        _clock = clock;
        _audit = audit;
        _current = current;
    }

    // ─── Öğrencinin kendisi (belge 2.4) ───────────────────────────────────────

    public async Task<StudentDetailDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);
        var student = await LoadMineAsync(ct);
        return student.ToDetail(revealPersonalData: true);   // kendi verisi: ham gösterilir
    }

    public async Task<StudentDetailDto> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.Student);
        var student = await LoadMineAsync(ct);

        var phone = InputNormalizer.Phone(request.PhoneNumber);
        var iban = IbanValidator.Normalize(request.Iban);

        var errors = new ValidationErrors();
        if (!InputNormalizer.IsValidMobile(phone))
            errors.Add(nameof(request.PhoneNumber), "Geçerli bir cep telefonu numarası giriniz (örn. 0532 123 45 67).");
        if (!IbanValidator.IsValid(iban))
            errors.Add(nameof(request.Iban), "IBAN geçersiz. TR ile başlayan, 26 haneli geçerli bir IBAN giriniz.");
        errors.ThrowIfAny();

        student.User.PhoneNumber = phone;
        student.Iban = iban;

        // Denetim kaydına yeni değerler YAZILMAZ (kişisel veri); yalnızca "güncellendi" bilgisi tutulur.
        await _audit.RecordAsync(AuditActions.StudentProfileUpdated, nameof(StudentProfile), student.Id,
            "Telefon ve IBAN güncellendi.", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return student.ToDetail(revealPersonalData: true);
    }

    // ─── Personel: listeleme ve görüntüleme ───────────────────────────────────

    public async Task<PagedResult<StudentSummaryDto>> SearchAsync(StudentSearchQuery query, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor);

        var criteria = new StudentSearchCriteria
        {
            DepartmentId = query.DepartmentId,
            AdvisorUserId = query.AdvisorUserId,
            WorkUnitId = query.WorkUnitId,
            RegistrationYear = query.RegistrationYear,
            IsActive = query.IsActive,
            Text = query.Text
        };

        // Rol kapsamı kullanıcının gönderdiği filtrenin ÜZERİNE yazılır: bölüm başkanı başka bölümün
        // Id'sini yollasa bile kendi bölümüyle sınırlanır. Yetki sınırı asla istemciye bırakılmaz.
        switch (_current.Role)
        {
            case UserRole.DepartmentHead:
                criteria.DepartmentId = _current.DepartmentId
                    ?? throw new ForbiddenException("Hesabınıza bir bölüm atanmamış.");
                break;
            case UserRole.Advisor:
                criteria.AdvisorUserId = _current.UserId;
                break;
        }

        var page = await _students.SearchAsync(criteria,
            new PageRequest { Page = query.Page, PageSize = query.PageSize }, ct);

        return page.Map(s => s.ToSummary());
    }

    public async Task<StudentDetailDto> GetByIdAsync(int studentId, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead, UserRole.Advisor, UserRole.Student);

        var student = await _students.GetDetailAsync(studentId, ct)
            ?? throw new NotFoundException("Öğrenci bulunamadı.");
        EnsureCanView(student);

        // Ham TC/IBAN yalnızca öğrencinin kendisine ve fakülte yönetimine gösterilir.
        var reveal = _current.Role is UserRole.FacultyAdmin or UserRole.Student;

        // Başkasının ham kişisel verisini görmek denetim kaydına yazılır (belge madde 11).
        if (reveal && _current.Role == UserRole.FacultyAdmin)
        {
            await _audit.RecordAsync(AuditActions.PersonalDataViewed, nameof(StudentProfile), student.Id, ct: ct);
            await _uow.SaveChangesAsync(ct);
        }

        return student.ToDetail(reveal);
    }

    // ─── Yönetim işlemleri ────────────────────────────────────────────────────

    public async Task<StudentDetailDto> EndParticipationAsync(int studentId, EndParticipationRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var student = await LoadAsync(studentId, ct);

        var open = student.CurrentParticipation
            ?? throw new ConflictException("Öğrenci zaten pasif durumda.");

        var today = _clock.Today;
        var endDate = request.EndDate ?? today;
        if (endDate < open.StartDate)
            throw new DomainValidationException(nameof(request.EndDate), "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        if (endDate > today)
            throw new DomainValidationException(nameof(request.EndDate), "Bitiş tarihi gelecekte olamaz.");

        // Kayıt SİLİNMEZ; yalnızca bitiş tarihi ve gerekçe yazılır (belge madde 9).
        open.EndDate = endDate;
        open.EndReason = request.Reason.Trim();

        // Programdan ayrılan öğrencinin açık birim/danışman atamaları da kapanır; geçmiş olarak kalır.
        if (student.CurrentUnitAssignment is { } unit)
            unit.EndDate = Latest(endDate, unit.StartDate);
        if (student.CurrentAdvisorAssignment is { } advisor)
            advisor.EndDate = Latest(endDate, advisor.StartDate);

        await _audit.RecordAsync(AuditActions.ParticipationEnded, nameof(StudentProfile), student.Id,
            "Öğrencinin program katılımı sonlandırıldı.", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return student.ToDetail(revealPersonalData: true);
    }

    public async Task<StudentDetailDto> StartParticipationAsync(int studentId, StartParticipationRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var student = await LoadAsync(studentId, ct);

        if (student.CurrentParticipation is not null)
            throw new ConflictException("Öğrenci zaten aktif durumda.");

        var today = _clock.Today;
        var startDate = request.StartDate ?? today;
        if (startDate > today)
            throw new DomainValidationException(nameof(request.StartDate), "Başlangıç tarihi gelecekte olamaz.");

        // Dönemler çakışamaz: yeni dönem, bir önceki dönemin bitişinden sonra başlamalı.
        var lastEnd = student.Participations
            .Where(p => p.EndDate.HasValue)
            .Select(p => p.EndDate!.Value)
            .DefaultIfEmpty(DateOnly.MinValue)
            .Max();
        if (startDate < lastEnd)
            throw new DomainValidationException(nameof(request.StartDate),
                "Yeni katılım dönemi, önceki dönemin bitiş tarihinden önce başlayamaz.");

        student.Participations.Add(new ProgramParticipation { StartDate = startDate });

        await _audit.RecordAsync(AuditActions.ParticipationStarted, nameof(StudentProfile), student.Id,
            "Öğrenci programa yeniden dahil edildi.", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return student.ToDetail(revealPersonalData: true);
    }

    public async Task<StudentDetailDto> AssignUnitAsync(int studentId, AssignUnitRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var student = await LoadAsync(studentId, ct);

        if (!student.IsActive)
            throw new ConflictException("Pasif durumdaki öğrenciye birim atanamaz.");

        var unit = await _units.GetByIdAsync(request.WorkUnitId)
            ?? throw new NotFoundException("Birim bulunamadı.");
        if (!unit.IsActive)
            throw new DomainValidationException(nameof(request.WorkUnitId), "Seçilen birim aktif değil.");

        var current = student.CurrentUnitAssignment;
        if (current?.WorkUnitId == unit.Id)
            throw new ConflictException("Öğrenci zaten bu birimde görevli.");

        var today = _clock.Today;

        // "Eski atamayı kapat + yenisini ekle": benzersiz indeks (aynı anda tek açık atama) yüzünden
        // kapatma önce kaydedilir; ikisi aynı işlemde olduğu için ortada hata olursa ikisi de geri alınır.
        await using var tx = await _uow.BeginTransactionAsync(ct);

        if (current is not null)
        {
            current.EndDate = Latest(today, current.StartDate);
            await _uow.SaveChangesAsync(ct);
        }

        student.UnitAssignments.Add(new StudentUnitAssignment
        {
            WorkUnit = unit,
            StartDate = today,
            AssignedByUserId = _current.UserId
        });

        await _audit.RecordAsync(AuditActions.UnitAssigned, nameof(StudentProfile), student.Id,
            $"WorkUnitId={unit.Id}", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return student.ToDetail(revealPersonalData: true);
    }

    public async Task<StudentDetailDto> AssignAdvisorAsync(int studentId, AssignAdvisorRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);
        var student = await LoadAsync(studentId, ct);

        // Bölüm başkanı yalnızca kendi bölümünün öğrencisine danışman atayabilir.
        if (_current.Role == UserRole.DepartmentHead && _current.DepartmentId != student.DepartmentId)
            throw new NotFoundException("Öğrenci bulunamadı.");

        if (!student.IsActive)
            throw new ConflictException("Pasif durumdaki öğrenciye danışman atanamaz.");

        var advisor = await _users.GetWithDepartmentAsync(request.AdvisorUserId, ct);
        if (advisor is null || advisor.Role != UserRole.Advisor || !advisor.IsActive)
            throw new DomainValidationException(nameof(request.AdvisorUserId), "Seçilen kullanıcı aktif bir danışman değil.");
        if (advisor.DepartmentId != student.DepartmentId)
            throw new DomainValidationException(nameof(request.AdvisorUserId), "Danışman, öğrenciyle aynı bölümde olmalıdır.");

        var current = student.CurrentAdvisorAssignment;
        if (current?.AdvisorUserId == advisor.Id)
            throw new ConflictException("Bu kişi zaten öğrencinin danışmanı.");

        var today = _clock.Today;

        await using var tx = await _uow.BeginTransactionAsync(ct);

        if (current is not null)
        {
            current.EndDate = Latest(today, current.StartDate);
            await _uow.SaveChangesAsync(ct);
        }

        student.AdvisorAssignments.Add(new AdvisorAssignment
        {
            AdvisorUser = advisor,
            StartDate = today,
            AssignedByUserId = _current.UserId
        });

        await _audit.RecordAsync(AuditActions.AdvisorAssigned, nameof(StudentProfile), student.Id,
            $"AdvisorUserId={advisor.Id}", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return student.ToDetail(revealPersonalData: _current.Role == UserRole.FacultyAdmin);
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<StudentProfile> LoadMineAsync(CancellationToken ct)
        => await _students.GetDetailByUserIdAsync(_current.UserId, ct)
           ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

    private async Task<StudentProfile> LoadAsync(int studentId, CancellationToken ct)
        => await _students.GetDetailAsync(studentId, ct)
           ?? throw new NotFoundException("Öğrenci bulunamadı.");

    /// <summary>
    /// Kapsam dışı öğrenci için 403 yerine 404 döner: "bu Id var ama yetkin yok" bilgisi
    /// sızdırılmaz, Id taramasıyla öğrenci varlığı keşfedilemez.
    /// </summary>
    private void EnsureCanView(StudentProfile student)
    {
        var allowed = _current.Role switch
        {
            UserRole.FacultyAdmin => true,
            UserRole.DepartmentHead => _current.DepartmentId == student.DepartmentId,
            UserRole.Advisor => student.CurrentAdvisorAssignment?.AdvisorUserId == _current.UserId,
            UserRole.Student => student.UserId == _current.UserId,
            _ => false
        };

        if (!allowed)
            throw new NotFoundException("Öğrenci bulunamadı.");
    }

    /// <summary>Bitiş tarihinin başlangıçtan önceye düşmemesi için iki tarihten büyüğünü verir.</summary>
    private static DateOnly Latest(DateOnly a, DateOnly b) => a >= b ? a : b;
}
