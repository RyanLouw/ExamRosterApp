namespace ExamRosterApp.Domain.Entities;
public class Teacher { public int Id {get;set;} public string FullName {get;set;} = string.Empty; public bool CanWorkMorning {get;set;}=true; public bool CanWorkAfternoon {get;set;}=true; public bool IsActive {get;set;}=true; public string? Notes {get;set;} }
