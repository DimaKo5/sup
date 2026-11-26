using System.Diagnostics;

namespace CollegeSupport.Core.Entities;

/// <summary>Сотрудник колледжа — справочник, по которому создаются обращения.</summary>
[DebuggerDisplay("{FullName} ({Phone}), кабинет {Cabinet}")]
public class Employee
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Cabinet { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Appeal> Appeals { get; set; } = new();
}
