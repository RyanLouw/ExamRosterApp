namespace ExamRosterApp.Domain.Entities;
public class DutyAssignment { public int Id {get;set;} public int TeacherId {get;set;} public int ExamDutySlotId {get;set;} public DateTime AssignedOn {get;set;}=DateTime.UtcNow; public bool IsManualOverride {get;set;} public Teacher? Teacher {get;set;} public ExamDutySlot? ExamDutySlot {get;set;} }
