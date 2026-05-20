namespace ExamRosterApp.Domain.Entities;
public class TeacherUnavailableSlot { public int Id {get;set;} public int TeacherId {get;set;} public DateOnly Date {get;set;} public TimeOnly StartTime {get;set;} public TimeOnly EndTime {get;set;} public string? Reason {get;set;} public Teacher? Teacher {get;set;} }
