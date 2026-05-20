namespace ExamRosterApp.Domain;

public enum ShiftType { Morning, Afternoon }

public class Teacher { public int Id { get; set; } public string FullName { get; set; } = string.Empty; public bool CanWorkMorning { get; set; } = true; public bool CanWorkAfternoon { get; set; } = true; public bool IsActive { get; set; } = true; public string? Notes { get; set; } }
public class ExamDutySlot { public int Id { get; set; } public DateOnly Date { get; set; } public TimeOnly StartTime { get; set; } public TimeOnly EndTime { get; set; } public ShiftType ShiftType { get; set; } public string Grade { get; set; } = string.Empty; public string Subject { get; set; } = string.Empty; public string Venue { get; set; } = string.Empty; public int TeachersRequired { get; set; } = 1; public int DurationMinutes => (int)(EndTime.ToTimeSpan()-StartTime.ToTimeSpan()).TotalMinutes; }
public class DutyAssignment { public int Id { get; set; } public int TeacherId { get; set; } public int ExamDutySlotId { get; set; } public DateTime AssignedOn { get; set; } = DateTime.UtcNow; public bool IsManualOverride { get; set; } public Teacher? Teacher { get; set; } public ExamDutySlot? ExamDutySlot { get; set; } }
public class TeacherUnavailableSlot { public int Id { get; set; } public int TeacherId { get; set; } public DateOnly Date { get; set; } public TimeOnly StartTime { get; set; } public TimeOnly EndTime { get; set; } public string Reason { get; set; } = string.Empty; }
