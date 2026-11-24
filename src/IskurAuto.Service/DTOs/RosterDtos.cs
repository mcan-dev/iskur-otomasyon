namespace IskurAuto.Service.DTOs;

/// <summary>Bir ayda hangi gün hangi öğrencilerin çalıştığı (onaylı plana göre) ve devam/izin durumu.</summary>
public class WorkRosterDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<WorkRosterDayDto> Days { get; set; } = new();
}

public class WorkRosterDayDto
{
    public DateOnly Date { get; set; }
    public List<WorkRosterEntryDto> Students { get; set; } = new();
}

public class WorkRosterEntryDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string? UnitName { get; set; }

    /// <summary>Planned | Attended | OnLeave | Missing</summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>Seçilen yılda çalışan öğrenciler (geçmiş yıllar dahil).</summary>
public class YearWorkersDto
{
    public int Year { get; set; }
    public List<YearWorkerDto> Students { get; set; } = new();
}

public class YearWorkerDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentNumber { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int PlannedDays { get; set; }
    public int WorkedDays { get; set; }
    public decimal LeaveDays { get; set; }
}

public class RosterQuery
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int? DepartmentId { get; set; }
}

public class YearWorkersQuery
{
    public int Year { get; set; }
    public int? DepartmentId { get; set; }
}
