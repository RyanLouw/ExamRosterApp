using ExamRosterApp.Domain.Entities; using Microsoft.EntityFrameworkCore;
namespace ExamRosterApp.Infrastructure.Data;
public class ExamRosterDbContext(DbContextOptions<ExamRosterDbContext> options):DbContext(options){
 public DbSet<Teacher> Teachers => Set<Teacher>(); public DbSet<ExamDutySlot> ExamDutySlots => Set<ExamDutySlot>(); public DbSet<DutyAssignment> DutyAssignments => Set<DutyAssignment>(); public DbSet<TeacherUnavailableSlot> TeacherUnavailableSlots => Set<TeacherUnavailableSlot>();
 protected override void OnModelCreating(ModelBuilder b){
  b.Entity<DutyAssignment>().HasOne(x=>x.Teacher).WithMany().HasForeignKey(x=>x.TeacherId);
  b.Entity<DutyAssignment>().HasOne(x=>x.ExamDutySlot).WithMany().HasForeignKey(x=>x.ExamDutySlotId);
  b.Entity<Teacher>().HasData(new Teacher{Id=1,FullName="Alice Jones",CanWorkMorning=true,CanWorkAfternoon=false,IsActive=true},new Teacher{Id=2,FullName="Bob Smith",CanWorkMorning=true,CanWorkAfternoon=true,IsActive=true},new Teacher{Id=3,FullName="Carla Diaz",CanWorkMorning=false,CanWorkAfternoon=true,IsActive=true});
  b.Entity<ExamDutySlot>().HasData(new ExamDutySlot{Id=1,Date=new DateOnly(2026,6,1),StartTime=new TimeOnly(9,0),EndTime=new TimeOnly(11,0),ShiftType=Domain.Enums.ShiftType.Morning,Grade="10",Subject="Math",Venue="Hall A",TeachersRequired=2},new ExamDutySlot{Id=2,Date=new DateOnly(2026,6,1),StartTime=new TimeOnly(13,0),EndTime=new TimeOnly(15,0),ShiftType=Domain.Enums.ShiftType.Afternoon,Grade="11",Subject="Physics",Venue="Hall B",TeachersRequired=2});
 }
}
