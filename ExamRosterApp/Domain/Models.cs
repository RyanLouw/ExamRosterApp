namespace ExamRosterApp.Domain;

public enum ShiftType
{
    Morning = 1,
    Afternoon = 2
}

public sealed class Teacher
{
    public required int Id { get; init; }
    public required string FullName { get; init; }
    public bool CanWorkMorning { get; init; } = true;
    public bool CanWorkAfternoon { get; init; } = true;
    public int MaxDutiesPerDay { get; init; } = 2;
    public IReadOnlyList<UnavailableSlot> UnavailableSlots { get; init; } = [];
}

public sealed class UnavailableSlot
{
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
}

public sealed class ExamDutySlot
{
    public required int Id { get; init; }
    public required DateOnly Date { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public required ShiftType ShiftType { get; init; }
    public required string Grade { get; init; }
    public required string Subject { get; init; }
    public required string Venue { get; init; }
    public required int TeachersRequired { get; init; }

    public int DurationMinutes =>
        (int)(EndTime.ToTimeSpan() - StartTime.ToTimeSpan()).TotalMinutes;
}

public sealed class DutyAssignment
{
    public required int TeacherId { get; init; }
    public required int ExamDutySlotId { get; init; }
}

public sealed class TeacherDutyStats
{
    public required int TeacherId { get; init; }
    public int TotalMinutes { get; set; }
    public int MorningMinutes { get; set; }
    public int AfternoonMinutes { get; set; }
    public int DutyCount { get; set; }
}
