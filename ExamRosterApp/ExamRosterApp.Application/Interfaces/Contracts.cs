using ExamRosterApp.Application.DTOs; using ExamRosterApp.Domain.Entities;
namespace ExamRosterApp.Application.Interfaces;
public interface IRepository { Task<List<Teacher>> GetTeachersAsync(); Task<List<ExamDutySlot>> GetExamDutySlotsAsync(); Task<List<TeacherUnavailableSlot>> GetUnavailableAsync(); Task<List<DutyAssignment>> GetAssignmentsAsync(); Task SaveAssignmentsAsync(List<DutyAssignment> assignments); Task ClearAssignmentsAsync(); Task SaveChangesAsync(); }
public interface IRosterGenerator { Task<(List<DutyAssignment> Assignments,List<RosterWarning> Warnings)> GenerateRosterAsync(List<Teacher> teachers,List<ExamDutySlot> slots,List<TeacherUnavailableSlot> unavailable,List<DutyAssignment> existing); }
public interface ITeacherService { Task<List<Teacher>> GetTeachersAsync(); Task AddTeacherAsync(Teacher teacher); Task UpdateTeacherAsync(Teacher teacher); Task DeactivateTeacherAsync(int teacherId); }
public interface IExamDutySlotService { Task<List<ExamDutySlot>> GetExamDutySlotsAsync(); Task AddExamDutySlotAsync(ExamDutySlot slot); Task UpdateExamDutySlotAsync(ExamDutySlot slot); Task DeleteExamDutySlotAsync(int id); }
public interface IRosterService { Task<List<RosterWarning>> GenerateAndSaveRosterAsync(); Task<List<DutyAssignmentView>> GetCurrentRosterAsync(); Task ClearRosterAsync(); Task<List<TeacherDutySummaryView>> GetTeacherDutySummaryAsync(); }
