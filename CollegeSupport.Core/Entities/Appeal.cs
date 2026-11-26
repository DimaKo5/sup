using System.Diagnostics;

namespace CollegeSupport.Core.Entities;

/// <summary>Обращение в техническую поддержку колледжа.</summary>
[DebuggerDisplay("Обращение {Id}: {Equipment}, {Status}")]
public class Appeal
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string Cabinet { get; set; } = string.Empty;
    public string Equipment { get; set; } = string.Empty;
    public string ProblemDescription { get; set; } = string.Empty;

    public AppealStatus Status { get; set; } = AppealStatus.New;

    public int? TechnicianId { get; set; }
    public User? Technician { get; set; }

    public decimal? Cost { get; set; }
    public DateTime? CompletedAt { get; set; }
}
