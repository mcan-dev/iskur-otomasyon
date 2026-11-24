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
    private readonly IDepartmentRepository _departments;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly IAuditService _audit;
    private readonly ICurrentUser _current;

    public StudentService(
        IStudentRepository students, IUserRepository users, IWorkUnitRepository units,
        IDepartmentRepository departments, IPasswordHasher hasher,
        IUnitOfWork uow, IClock clock, IAuditService audit, ICurrentUser current)
    {
        _departments = departments;
        _hasher = hasher;
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
        else
            errors.Add(nameof(request.Iban), IbanBankRules.VirtualBankError(iban));
        if (!IbanBankRules.HolderMatches(request.IbanHolderName, student.User.FullName))
            errors.Add(nameof(request.IbanHolderName), "IBAN sizin adınıza olmalıdır: hesap sahibi adı soyadı, kayıtlı adınızla aynı olmalı.");
        if (!request.ConfirmOwnIban)
            errors.Add(nameof(request.ConfirmOwnIban), "IBAN'ın size ait olduğunu onaylamalısınız.");
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
        _current.EnsureCanView(student);

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

    public async Task<StudentDetailDto> SetLeaveAllowanceAsync(int studentId, SetLeaveAllowanceRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var student = await LoadAsync(studentId, ct);

        student.LeaveDayAllowance = request.Days;

        await _audit.RecordAsync(AuditActions.LeaveAllowanceChanged, nameof(StudentProfile), student.Id,
            request.Days is int days ? $"Öğrenciye özel izin hakkı {days} gün olarak belirlendi." : "Öğrenci genel izin hakkına döndürüldü.", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return student.ToDetail(revealPersonalData: true);
    }

    public async Task<StudentDetailDto> AssignUnitAsync(int studentId, AssignUnitRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var student = await LoadAsync(studentId, ct);
        var unit = await ResolveUnitAsync(request.WorkUnitId);

        await AssignUnitCoreAsync(student, unit, ct);
        return student.ToDetail(revealPersonalData: true);
    }

    public async Task<StudentDetailDto> AssignAdvisorAsync(int studentId, AssignAdvisorRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);
        var student = await LoadAsync(studentId, ct);

        // Birim yetkilisi yalnızca kendi bölümünün öğrencisine sorumlu personel atayabilir.
        if (_current.Role == UserRole.DepartmentHead && _current.DepartmentId != student.DepartmentId)
            throw new NotFoundException("Öğrenci bulunamadı.");

        var advisor = await ResolveAdvisorAsync(request.AdvisorUserId, request.AdvisorRegistryNumber, ct);
        await AssignAdvisorCoreAsync(student, advisor, ct);

        return student.ToDetail(revealPersonalData: _current.Role == UserRole.FacultyAdmin);
    }

    /// <summary>
    /// Seçilen öğrencilerin hepsini tek işlemde birime ve/veya sorumlu personele atar. Atanamayan öğrenciler
    /// (pasif, farklı bölüm, zaten aynı yerde) atlanır ve nedenleriyle birlikte döner.
    /// </summary>
    public async Task<BulkAssignResultDto> BulkAssignAsync(BulkAssignRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin, UserRole.DepartmentHead);

        var wantsAdvisor = request.AdvisorUserId is not null || !string.IsNullOrWhiteSpace(request.AdvisorRegistryNumber);
        if (request.WorkUnitId is null && !wantsAdvisor)
            throw new DomainValidationException("Atama", "Bir çalışma birimi veya sorumlu personel seçin.");
        if (request.WorkUnitId is not null && _current.Role != UserRole.FacultyAdmin)
            throw new ForbiddenException("Çalışma birimi atamasını yalnızca fakülte sekreterliği yapar.");

        var studentIds = (request.StudentIds ?? new List<int>()).Distinct().ToList();
        if (studentIds.Count == 0)
            throw new DomainValidationException(nameof(request.StudentIds), "En az bir öğrenci seçin.");

        var unit = request.WorkUnitId is int unitId ? await ResolveUnitAsync(unitId) : null;
        var advisor = wantsAdvisor ? await ResolveAdvisorAsync(request.AdvisorUserId, request.AdvisorRegistryNumber, ct) : null;

        var result = new BulkAssignResultDto();
        foreach (var id in studentIds)
        {
            var student = await _students.GetDetailAsync(id, ct);
            if (student is null || !_current.CanView(student))
            {
                result.Errors.Add(new BulkAssignErrorDto { StudentId = id, Message = "Öğrenci bulunamadı." });
                continue;
            }

            try
            {
                if (_current.Role == UserRole.DepartmentHead && _current.DepartmentId != student.DepartmentId)
                    throw new NotFoundException("Öğrenci bulunamadı.");

                var changed = false;
                if (unit is not null && student.CurrentUnitAssignment?.WorkUnitId != unit.Id)
                {
                    await AssignUnitCoreAsync(student, unit, ct);
                    changed = true;
                }
                if (advisor is not null && student.CurrentAdvisorAssignment?.AdvisorUserId != advisor.Id)
                {
                    await AssignAdvisorCoreAsync(student, advisor, ct);
                    changed = true;
                }

                if (changed) result.Assigned++;
                else result.Errors.Add(new BulkAssignErrorDto { StudentId = id, StudentName = student.User.FullName, Message = "Zaten bu atamaya sahip." });
            }
            catch (Exception ex) when (ex is ConflictException or DomainValidationException or NotFoundException)
            {
                result.Errors.Add(new BulkAssignErrorDto { StudentId = id, StudentName = student.User.FullName, Message = ex.Message });
            }
        }

        return result;
    }

    // ─── Öğrenci hesabı açma ve düzenleme (sekreterlik) ───────────────────────

    public async Task<StudentDetailDto> CreateAsync(CreateStudentRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);

        var email = InputNormalizer.Email(request.Email);
        var fullName = InputNormalizer.Name(request.FullName);
        var phone = InputNormalizer.Phone(request.PhoneNumber);
        var nationalId = (request.NationalId ?? string.Empty).Trim();
        var studentNumber = (request.StudentNumber ?? string.Empty).Trim();

        var errors = new ValidationErrors();
        if (fullName.Length < 2) errors.Add(nameof(request.FullName), "Ad soyad en az 2 karakter olmalıdır.");
        if (string.IsNullOrWhiteSpace(request.ObisPassword) || request.ObisPassword.Length < 4)
            errors.Add(nameof(request.ObisPassword), "OBİS şifresi en az 4 karakter olmalıdır.");
        if (!NationalIdValidator.IsValid(nationalId))
            errors.Add(nameof(request.NationalId), "T.C. kimlik numarası geçersiz.");
        if (!InputNormalizer.IsValidMobile(phone))
            errors.Add(nameof(request.PhoneNumber), "Geçerli bir cep telefonu numarası giriniz (örn. 0532 123 45 67).");
        if (studentNumber.Length is < 3 or > 9)
            errors.Add(nameof(request.StudentNumber), "Öğrenci numarası 3 ile 9 karakter arasında olmalıdır.");
        if (request.RegistrationYear > _clock.Today.Year)
            errors.Add(nameof(request.RegistrationYear), "Kayıt yılı içinde bulunulan yıldan sonra olamaz.");

        var department = await _departments.GetByIdAsync(request.DepartmentId);
        if (department is null || !department.IsActive)
            errors.Add(nameof(request.DepartmentId), "Seçilen bölüm bulunamadı.");
        errors.ThrowIfAny();

        if (await _users.EmailExistsAsync(email, ct))
            throw new ConflictException("Bu e-posta adresiyle kayıtlı bir hesap zaten var.");
        if (await _students.NationalIdExistsAsync(nationalId, ct: ct))
            throw new ConflictException("Bu T.C. kimlik numarasıyla kayıtlı bir öğrenci zaten var.");
        if (await _students.StudentNumberExistsAsync(studentNumber, ct: ct))
            throw new ConflictException("Bu öğrenci numarasıyla kayıtlı bir öğrenci zaten var.");

        var profile = new StudentProfile
        {
            User = new User
            {
                FullName = fullName,
                Email = email,
                PhoneNumber = phone,
                PasswordHash = _hasher.Hash(request.ObisPassword),
                Role = UserRole.Student,
                IsActive = true
            },
            NationalId = nationalId,
            StudentNumber = studentNumber,
            DepartmentId = department!.Id,
            RegistrationYear = request.RegistrationYear
        };
        profile.Participations.Add(new ProgramParticipation { StartDate = _clock.Today });
        await _students.AddAsync(profile);

        await using var tx = await _uow.BeginTransactionAsync(ct);
        await _uow.SaveChangesAsync(ct);
        await _audit.RecordAsync(AuditActions.StudentCreated, nameof(StudentProfile), profile.Id,
            "Öğrenci hesabı bilgi işlem bilgileriyle açıldı.", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return (await LoadAsync(profile.Id, ct)).ToDetail(revealPersonalData: true);
    }

    public async Task<StudentDetailDto> UpdateAsync(int studentId, UpdateStudentRequest request, CancellationToken ct = default)
    {
        _current.RequireRole(UserRole.FacultyAdmin);
        var student = await LoadAsync(studentId, ct);

        var email = InputNormalizer.Email(request.Email);
        var fullName = InputNormalizer.Name(request.FullName);
        var phone = InputNormalizer.Phone(request.PhoneNumber);
        var nationalId = (request.NationalId ?? string.Empty).Trim();
        var studentNumber = (request.StudentNumber ?? string.Empty).Trim();
        var iban = IbanValidator.Normalize(request.Iban);

        var errors = new ValidationErrors();
        if (fullName.Length < 2) errors.Add(nameof(request.FullName), "Ad soyad en az 2 karakter olmalıdır.");
        if (!NationalIdValidator.IsValid(nationalId))
            errors.Add(nameof(request.NationalId), "T.C. kimlik numarası geçersiz.");
        if (!InputNormalizer.IsValidMobile(phone))
            errors.Add(nameof(request.PhoneNumber), "Geçerli bir cep telefonu numarası giriniz (örn. 0532 123 45 67).");
        if (studentNumber.Length is < 3 or > 9)
            errors.Add(nameof(request.StudentNumber), "Öğrenci numarası 3 ile 9 karakter arasında olmalıdır.");
        if (iban.Length > 0)
        {
            if (!IbanValidator.IsValid(iban))
                errors.Add(nameof(request.Iban), "IBAN geçersiz. TR ile başlayan, 26 haneli geçerli bir IBAN giriniz.");
            else
                errors.Add(nameof(request.Iban), IbanBankRules.VirtualBankError(iban));
        }
        if (!string.IsNullOrEmpty(request.NewObisPassword) && request.NewObisPassword.Length < 4)
            errors.Add(nameof(request.NewObisPassword), "OBİS şifresi en az 4 karakter olmalıdır.");

        var department = await _departments.GetByIdAsync(request.DepartmentId);
        if (department is null)
            errors.Add(nameof(request.DepartmentId), "Seçilen bölüm bulunamadı.");
        errors.ThrowIfAny();

        if (await _users.EmailExistsExceptAsync(email, student.UserId, ct))
            throw new ConflictException("Bu e-posta adresiyle kayıtlı başka bir hesap var.");
        if (await _students.NationalIdExistsAsync(nationalId, student.Id, ct))
            throw new ConflictException("Bu T.C. kimlik numarasıyla kayıtlı başka bir öğrenci var.");
        if (await _students.StudentNumberExistsAsync(studentNumber, student.Id, ct))
            throw new ConflictException("Bu öğrenci numarasıyla kayıtlı başka bir öğrenci var.");

        // Bölüm değişirse eski bölümün sorumlu personeli artık bu öğrencinin sorumlusu olamaz: atama kapanır.
        if (student.DepartmentId != department!.Id && student.CurrentAdvisorAssignment is { } advisor)
            advisor.EndDate = Latest(_clock.Today, advisor.StartDate);

        student.User.FullName = fullName;
        student.User.Email = email;
        student.User.PhoneNumber = phone;
        student.NationalId = nationalId;
        student.StudentNumber = studentNumber;
        student.DepartmentId = department.Id;
        student.Department = department;
        student.RegistrationYear = request.RegistrationYear;
        if (iban.Length > 0) student.Iban = iban;
        if (!string.IsNullOrEmpty(request.NewObisPassword))
            student.User.PasswordHash = _hasher.Hash(request.NewObisPassword);

        // Yeni değerler denetim kaydına yazılmaz (kişisel veri).
        await _audit.RecordAsync(AuditActions.StudentUpdated, nameof(StudentProfile), student.Id,
            "Öğrenci bilgileri sekreterlik tarafından güncellendi.", ct: ct);
        await _uow.SaveChangesAsync(ct);

        return student.ToDetail(revealPersonalData: true);
    }

    // ─── Atama yardımcıları ───────────────────────────────────────────────────

    private async Task<WorkUnit> ResolveUnitAsync(int workUnitId)
    {
        var unit = await _units.GetByIdAsync(workUnitId)
            ?? throw new NotFoundException("Birim bulunamadı.");
        if (!unit.IsActive)
            throw new DomainValidationException(nameof(AssignUnitRequest.WorkUnitId), "Seçilen birim aktif değil.");
        return unit;
    }

    /// <summary>Sorumlu personeli Id'si ya da sicil numarasıyla bulur.</summary>
    private async Task<User> ResolveAdvisorAsync(int? advisorUserId, string? registryNumber, CancellationToken ct)
    {
        User? advisor = null;
        if (advisorUserId is int id)
            advisor = await _users.GetWithDepartmentAsync(id, ct);
        else if (!string.IsNullOrWhiteSpace(registryNumber))
            advisor = await _users.GetByRegistryNumberAsync(InputNormalizer.RegistryNumber(registryNumber), ct);
        else
            throw new DomainValidationException(nameof(AssignAdvisorRequest.AdvisorRegistryNumber), "Sorumlu personelin sicil numarasını girin.");

        if (advisor is null || advisor.Role != UserRole.Advisor || !advisor.IsActive)
            throw new DomainValidationException(nameof(AssignAdvisorRequest.AdvisorRegistryNumber),
                "Bu sicil numarasında aktif bir sorumlu personel bulunamadı.");
        return advisor;
    }

    private async Task AssignUnitCoreAsync(StudentProfile student, WorkUnit unit, CancellationToken ct)
    {
        if (!student.IsActive)
            throw new ConflictException("Pasif durumdaki öğrenciye birim atanamaz.");

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
            $"Çalışma birimi: {unit.Name}", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    private async Task AssignAdvisorCoreAsync(StudentProfile student, User advisor, CancellationToken ct)
    {
        if (!student.IsActive)
            throw new ConflictException("Pasif durumdaki öğrenciye sorumlu personel atanamaz.");
        if (advisor.DepartmentId != student.DepartmentId)
            throw new DomainValidationException(nameof(AssignAdvisorRequest.AdvisorRegistryNumber),
                "Sorumlu personel, öğrenciyle aynı bölümde olmalıdır.");

        var current = student.CurrentAdvisorAssignment;
        if (current?.AdvisorUserId == advisor.Id)
            throw new ConflictException("Bu kişi zaten öğrencinin sorumlu personeli.");

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
            $"Sorumlu personel: {advisor.FullName} (sicil {advisor.RegistryNumber})", ct: ct);
        await _uow.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    // ─── Yardımcılar ──────────────────────────────────────────────────────────

    private async Task<StudentProfile> LoadMineAsync(CancellationToken ct)
        => await _students.GetDetailByUserIdAsync(_current.UserId, ct)
           ?? throw new NotFoundException("Öğrenci profiliniz bulunamadı.");

    private async Task<StudentProfile> LoadAsync(int studentId, CancellationToken ct)
        => await _students.GetDetailAsync(studentId, ct)
           ?? throw new NotFoundException("Öğrenci bulunamadı.");

    /// <summary>Bitiş tarihinin başlangıçtan önceye düşmemesi için iki tarihten büyüğünü verir.</summary>
    private static DateOnly Latest(DateOnly a, DateOnly b) => a >= b ? a : b;
}
