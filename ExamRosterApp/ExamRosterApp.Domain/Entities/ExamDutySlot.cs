using ExamRosterApp.Domain.Enums;
namespace ExamRosterApp.Domain.Entities;
public class ExamDutySlot { public int Id {get;set;} public DateOnly Date {get;set;} public TimeOnly StartTime {get;set;} public TimeOnly EndTime {get;set;} public ShiftType ShiftType {get;set;} public string Grade {get;set;}=string.Empty; public string Subject {get;set;}=string.Empty; public string Venue {get;set;}=string.Empty; public int TeachersRequired {get;set;}=1; public int DurationMinutes => (int)(EndTime.ToTimeSpan()-StartTime.ToTimeSpan()).TotalMinutes; }
