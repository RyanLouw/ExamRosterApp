namespace ExamRosterApp.Rostering;

public enum ShiftType
{
    Morning = 1,
    Afternoon = 2
}

public enum TeacherGroup
{
    Open = 1,
    SecondaryNonPriority = 2,
    MainInactive = 3
}

public sealed class UnavailableSlot
{
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
}

public sealed class Teacher
{
    public int Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public bool CanWorkMorning { get; init; } = true;
    public bool CanWorkAfternoon { get; init; } = true;
    public TeacherGroup Group { get; init; } = TeacherGroup.Open;
    public string? HomeSubject { get; init; }
    public int? MinDutyMinutes { get; init; }
    public int? MaxDutyMinutes { get; init; }
    public List<UnavailableSlot> UnavailableSlots { get; init; } = [];
}

public sealed class ExamDutySlot
{
    public int Id { get; init; }
    public DateOnly Date { get; init; }
    public TimeOnly StartTime { get; init; }
    public TimeOnly EndTime { get; init; }
    public ShiftType ShiftType { get; init; }
    public string Grade { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Venue { get; init; } = string.Empty;
    public int TeachersRequired { get; init; }
    public int? MinTeachersRequired { get; init; }
    public int? MaxTeachersRequired { get; init; }
    public int? LearnerCount { get; init; }
    public int? LearnersPerInvigilator { get; init; }

    public int DurationMinutes => (int)(EndTime.ToTimeSpan() - StartTime.ToTimeSpan()).TotalMinutes;
}

public sealed class DutyAssignment
{
    public int TeacherId { get; init; }
    public int ExamDutySlotId { get; init; }
}

public sealed class RosterResult
{
    public List<DutyAssignment> Assignments { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Diagnostics { get; } = [];
}

public sealed class TeacherDutyStats
{
    public int TeacherId { get; init; }
    public int TotalMinutes { get; set; }
    public int MorningMinutes { get; set; }
    public int AfternoonMinutes { get; set; }
    public int TotalDuties { get; set; }
    public string? LastVenue { get; set; }
    public DateOnly? LastDate { get; set; }
    public TimeOnly? LastEndTime { get; set; }
}
