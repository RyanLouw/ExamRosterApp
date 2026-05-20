using ExamRosterApp.Domain;

namespace ExamRosterApp.Application;

public record RosterResult(IReadOnlyList<DutyAssignment> Assignments, IReadOnlyList<string> Warnings);
public record TeacherDutySummary(string Teacher, int TotalMinutes, int MorningMinutes, int AfternoonMinutes, int Duties);

public interface IAppRepository
{
    Task<List<Teacher>> GetTeachersAsync();
    Task<List<ExamDutySlot>> GetSlotsAsync();
    Task<List<TeacherUnavailableSlot>> GetUnavailableAsync();
    Task<List<DutyAssignment>> GetAssignmentsAsync();
    Task<Teacher?> GetTeacherByIdAsync(int id);
    Task<ExamDutySlot?> GetSlotByIdAsync(int id);
    Task AddTeacherAsync(Teacher teacher);
    Task AddSlotAsync(ExamDutySlot slot);
    Task RemoveSlotAsync(ExamDutySlot slot);
    Task SaveAssignmentsAsync(IEnumerable<DutyAssignment> assignments);
    Task ClearAssignmentsAsync();
    Task SaveChangesAsync();
}

public interface IRosterGenerator { Task<RosterResult> GenerateRosterAsync(IEnumerable<Teacher> teachers, IEnumerable<ExamDutySlot> slots, IEnumerable<TeacherUnavailableSlot> unavail, IEnumerable<DutyAssignment> existing); }
public interface ITeacherService { Task<List<Teacher>> GetTeachersAsync(); Task AddTeacherAsync(Teacher t); Task UpdateTeacherAsync(Teacher t); Task DeactivateTeacherAsync(int id); }
public interface IExamDutySlotService { Task<List<ExamDutySlot>> GetExamDutySlotsAsync(); Task AddExamDutySlotAsync(ExamDutySlot s); Task UpdateExamDutySlotAsync(ExamDutySlot s); Task DeleteExamDutySlotAsync(int id); }
public interface IRosterService { Task<RosterResult> GenerateAndSaveRosterAsync(); Task<List<(ExamDutySlot Slot, Teacher Teacher)>> GetCurrentRosterAsync(); Task ClearRosterAsync(); Task<List<TeacherDutySummary>> GetTeacherDutySummaryAsync(); }
public interface IDatabaseSetupService { Task ApplyMigrationsAsync(); }
