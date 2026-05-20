using ExamRosterApp.Application;
using ExamRosterApp.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExamRosterApp.Infrastructure;

public class ExamRosterDbContext(DbContextOptions<ExamRosterDbContext> o) : DbContext(o)
{
    public DbSet<Teacher> Teachers => Set<Teacher>();
    public DbSet<ExamDutySlot> ExamDutySlots => Set<ExamDutySlot>();
    public DbSet<DutyAssignment> DutyAssignments => Set<DutyAssignment>();
    public DbSet<TeacherUnavailableSlot> TeacherUnavailableSlots => Set<TeacherUnavailableSlot>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Teacher>().HasData(
            new Teacher { Id = 1, FullName = "Alice" },
            new Teacher { Id = 2, FullName = "Bob" },
            new Teacher { Id = 3, FullName = "Carla", CanWorkAfternoon = false });

        b.Entity<ExamDutySlot>().HasData(
            new ExamDutySlot { Id = 1, Date = new DateOnly(2026, 6, 1), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0), ShiftType = ShiftType.Morning, Grade = "10", Subject = "Math", Venue = "Hall A", TeachersRequired = 2 },
            new ExamDutySlot { Id = 2, Date = new DateOnly(2026, 6, 1), StartTime = new TimeOnly(13, 0), EndTime = new TimeOnly(15, 0), ShiftType = ShiftType.Afternoon, Grade = "10", Subject = "Science", Venue = "Hall B", TeachersRequired = 1 });
    }
}

public class EfRepository(ExamRosterDbContext db) : IAppRepository
{
    public Task<List<Teacher>> GetTeachersAsync() => db.Teachers.ToListAsync();
    public Task<List<ExamDutySlot>> GetSlotsAsync() => db.ExamDutySlots.ToListAsync();
    public Task<List<TeacherUnavailableSlot>> GetUnavailableAsync() => db.TeacherUnavailableSlots.ToListAsync();
    public Task<List<DutyAssignment>> GetAssignmentsAsync() => db.DutyAssignments.ToListAsync();
    public Task<Teacher?> GetTeacherByIdAsync(int id) => db.Teachers.FirstOrDefaultAsync(x => x.Id == id);
    public Task<ExamDutySlot?> GetSlotByIdAsync(int id) => db.ExamDutySlots.FirstOrDefaultAsync(x => x.Id == id);
    public async Task AddTeacherAsync(Teacher teacher) => await db.Teachers.AddAsync(teacher);
    public async Task AddSlotAsync(ExamDutySlot slot) => await db.ExamDutySlots.AddAsync(slot);
    public Task RemoveSlotAsync(ExamDutySlot slot) { db.ExamDutySlots.Remove(slot); return Task.CompletedTask; }
    public async Task SaveAssignmentsAsync(IEnumerable<DutyAssignment> a) => await db.DutyAssignments.AddRangeAsync(a);
    public Task ClearAssignmentsAsync() { db.DutyAssignments.RemoveRange(db.DutyAssignments); return Task.CompletedTask; }
    public Task SaveChangesAsync() => db.SaveChangesAsync();
}

public class DatabaseSetupService(ExamRosterDbContext db) : IDatabaseSetupService
{
    public Task ApplyMigrationsAsync() => db.Database.MigrateAsync();
}
