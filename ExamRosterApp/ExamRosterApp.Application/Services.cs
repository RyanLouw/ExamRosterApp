using ExamRosterApp.Domain;

namespace ExamRosterApp.Application;

public class TeacherService(IAppRepository r) : ITeacherService
{
    public Task<List<Teacher>> GetTeachersAsync() => r.GetTeachersAsync();
    public async Task AddTeacherAsync(Teacher t) { await r.AddTeacherAsync(t); await r.SaveChangesAsync(); }
    public Task UpdateTeacherAsync(Teacher t) => r.SaveChangesAsync();
    public async Task DeactivateTeacherAsync(int id) { var t = await r.GetTeacherByIdAsync(id); if (t is null) return; t.IsActive = false; await r.SaveChangesAsync(); }
}

public class ExamDutySlotService(IAppRepository r) : IExamDutySlotService
{
    public Task<List<ExamDutySlot>> GetExamDutySlotsAsync() => r.GetSlotsAsync();
    public async Task AddExamDutySlotAsync(ExamDutySlot s) { await r.AddSlotAsync(s); await r.SaveChangesAsync(); }
    public Task UpdateExamDutySlotAsync(ExamDutySlot s) => r.SaveChangesAsync();
    public async Task DeleteExamDutySlotAsync(int id) { var s = await r.GetSlotByIdAsync(id); if (s is null) return; await r.RemoveSlotAsync(s); await r.SaveChangesAsync(); }
}

public class RosterService(IAppRepository r, IRosterGenerator g) : IRosterService
{
    public async Task<RosterResult> GenerateAndSaveRosterAsync()
    {
        var res = await g.GenerateRosterAsync(await r.GetTeachersAsync(), await r.GetSlotsAsync(), await r.GetUnavailableAsync(), await r.GetAssignmentsAsync());
        await r.SaveAssignmentsAsync(res.Assignments);
        await r.SaveChangesAsync();
        return res;
    }

    public async Task<List<(ExamDutySlot Slot, Teacher Teacher)>> GetCurrentRosterAsync()
    {
        var t = await r.GetTeachersAsync(); var s = await r.GetSlotsAsync(); var a = await r.GetAssignmentsAsync();
        return a.Join(s, x => x.ExamDutySlotId, y => y.Id, (x, y) => (x, y)).Join(t, x => x.x.TeacherId, y => y.Id, (x, y) => (x.y, y)).ToList();
    }

    public async Task ClearRosterAsync() { await r.ClearAssignmentsAsync(); await r.SaveChangesAsync(); }

    public async Task<List<TeacherDutySummary>> GetTeacherDutySummaryAsync()
    {
        var cur = await GetCurrentRosterAsync();
        return cur.GroupBy(x => x.Teacher.FullName).Select(g => new TeacherDutySummary(g.Key, g.Sum(x => x.Slot.DurationMinutes), g.Where(x => x.Slot.ShiftType == ShiftType.Morning).Sum(x => x.Slot.DurationMinutes), g.Where(x => x.Slot.ShiftType == ShiftType.Afternoon).Sum(x => x.Slot.DurationMinutes), g.Count())).ToList();
    }
}
