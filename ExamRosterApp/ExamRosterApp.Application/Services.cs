using ExamRosterApp.Domain;

namespace ExamRosterApp.Application;

public record RosterGenerationResult(IReadOnlyList<DutyAssignment> Assignments, IReadOnlyList<string> Warnings);
public record TeacherDutySummary(string Teacher, int TotalDutyTime, int MorningDutyTime, int AfternoonDutyTime, int NumberOfDuties);

public interface IRepository { Task<List<Teacher>> GetTeachersAsync(); Task<List<ExamDutySlot>> GetSlotsAsync(); Task<List<TeacherUnavailableSlot>> GetUnavailableAsync(); Task<List<DutyAssignment>> GetAssignmentsAsync(); Task SaveAssignmentsAsync(IEnumerable<DutyAssignment> assignments); Task ClearAssignmentsAsync(); Task SaveChangesAsync(); }
public interface IRosterGenerator { Task<RosterGenerationResult> GenerateRosterAsync(List<Teacher> teachers, List<ExamDutySlot> slots, List<TeacherUnavailableSlot> unavailable); }
public interface ITeacherService { Task<List<Teacher>> GetTeachersAsync(); Task AddTeacherAsync(Teacher teacher); Task UpdateTeacherAsync(Teacher teacher); Task DeactivateTeacherAsync(int id); }
public interface IExamDutySlotService { Task<List<ExamDutySlot>> GetExamDutySlotsAsync(); Task AddExamDutySlotAsync(ExamDutySlot slot); Task UpdateExamDutySlotAsync(ExamDutySlot slot); Task DeleteExamDutySlotAsync(int id); }
public interface IRosterService { Task<RosterGenerationResult> GenerateAndSaveRosterAsync(); Task<List<DutyAssignment>> GetCurrentRosterAsync(); Task ClearRosterAsync(); Task<List<TeacherDutySummary>> GetTeacherDutySummaryAsync(); }

public class RosterGenerator : IRosterGenerator
{
    private readonly Random _random = new();
    public Task<RosterGenerationResult> GenerateRosterAsync(List<Teacher> teachers, List<ExamDutySlot> slots, List<TeacherUnavailableSlot> unavailable)
    {
        var warnings = new List<string>(); var assignments = new List<DutyAssignment>();
        var active = teachers.Where(t=>t.IsActive).ToList();
        var stats=active.ToDictionary(t=>t.Id,_=> (total:0,m:0,a:0,count:0));
        foreach (var slot in slots.OrderBy(s=>s.Date).ThenBy(s=>s.StartTime))
        {
            for (int i=0;i<slot.TeachersRequired;i++)
            {
                var candidate = active.Select(t => new {t, score=Score(t,slot,assignments,slots,unavailable,stats)}).Where(x=>x.score<100000).OrderBy(x=>x.score+_random.NextDouble()).FirstOrDefault();
                if (candidate is null){ warnings.Add($"Could not fully staff {slot.Date} {slot.StartTime}-{slot.EndTime} ({slot.Subject})"); continue; }
                assignments.Add(new DutyAssignment{TeacherId=candidate.t.Id,ExamDutySlotId=slot.Id,AssignedOn=DateTime.UtcNow});
                var cur=stats[candidate.t.Id]; var dur=slot.DurationMinutes; stats[candidate.t.Id]=(cur.total+dur,cur.m+(slot.ShiftType==ShiftType.Morning?dur:0),cur.a+(slot.ShiftType==ShiftType.Afternoon?dur:0),cur.count+1);
            }
        }
        return Task.FromResult(new RosterGenerationResult(assignments,warnings));
    }
    private static double Score(Teacher t, ExamDutySlot slot, List<DutyAssignment> a, List<ExamDutySlot> slots, List<TeacherUnavailableSlot> u, Dictionary<int,(int total,int m,int a,int count)> stats){
        if (slot.ShiftType==ShiftType.Morning && !t.CanWorkMorning) return 999999; if (slot.ShiftType==ShiftType.Afternoon && !t.CanWorkAfternoon) return 999999;
        if (u.Any(x=>x.TeacherId==t.Id && x.Date==slot.Date && x.StartTime < slot.EndTime && slot.StartTime < x.EndTime)) return 999999;
        var sm=slots.ToDictionary(x=>x.Id);
        if (a.Any(x=>x.TeacherId==t.Id && sm.ContainsKey(x.ExamDutySlotId) && sm[x.ExamDutySlotId].Date==slot.Date && sm[x.ExamDutySlotId].StartTime<slot.EndTime && slot.StartTime<sm[x.ExamDutySlotId].EndTime)) return 999999;
        var s=stats[t.Id]; return s.total + (slot.ShiftType==ShiftType.Morning?s.m:s.a)*0.7 + s.count*10;
    }
}
