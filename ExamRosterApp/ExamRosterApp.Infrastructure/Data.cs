using ExamRosterApp.Application;
using ExamRosterApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExamRosterApp.Infrastructure;

public class ExamRosterDbContext(DbContextOptions<ExamRosterDbContext> options) : DbContext(options)
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ExamDutySlot> ExamDutySlots => Set<ExamDutySlot>();
    public DbSet<DutyAssignment> DutyAssignments => Set<DutyAssignment>();
    public DbSet<TeacherUnavailableSlot> TeacherUnavailableSlots => Set<TeacherUnavailableSlot>();
}

public class EfRepository(ExamRosterDbContext db) : IRepository
{
    public Task<List<Teacher>> GetTeachersAsync() => db.Teachers.AsNoTracking().ToListAsync();
    public Task<List<ExamDutySlot>> GetSlotsAsync() => db.ExamDutySlots.AsNoTracking().ToListAsync();
    public Task<List<TeacherUnavailableSlot>> GetUnavailableAsync() => db.TeacherUnavailableSlots.AsNoTracking().ToListAsync();
    public Task<List<DutyAssignment>> GetAssignmentsAsync() => db.DutyAssignments.Include(x=>x.Teacher).Include(x=>x.ExamDutySlot).AsNoTracking().ToListAsync();
    public async Task SaveAssignmentsAsync(IEnumerable<DutyAssignment> assignments){ await db.DutyAssignments.AddRangeAsync(assignments); await db.SaveChangesAsync(); }
    public async Task ClearAssignmentsAsync(){ db.DutyAssignments.RemoveRange(db.DutyAssignments); await db.SaveChangesAsync(); }
    public Task SaveChangesAsync()=>db.SaveChangesAsync();
}
