using ExamRosterApp.Domain.Enums;
namespace ExamRosterApp.Application.DTOs;
public record RosterWarning(string Message);
public record DutyAssignmentView(DateOnly Date, TimeOnly StartTime, TimeOnly EndTime, ShiftType Shift, string Grade, string Subject, string Venue, string TeacherName);
public record TeacherDutySummaryView(string Teacher, int TotalDutyMinutes, int MorningDutyMinutes, int AfternoonDutyMinutes, int NumberOfDuties);
public record RosterGenerationResult(IReadOnlyList<int> AssignmentTeacherIds, IReadOnlyList<RosterWarning> Warnings);
