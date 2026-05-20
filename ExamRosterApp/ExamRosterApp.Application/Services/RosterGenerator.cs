using ExamRosterApp.Application.DTOs; using ExamRosterApp.Application.Interfaces; using ExamRosterApp.Domain.Entities; using ExamRosterApp.Domain.Enums;
namespace ExamRosterApp.Application.Services;
public class RosterGenerator : IRosterGenerator {
 public Task<(List<DutyAssignment> Assignments, List<RosterWarning> Warnings)> GenerateRosterAsync(List<Teacher> teachers,List<ExamDutySlot> slots,List<TeacherUnavailableSlot> unavailable,List<DutyAssignment> existing){
  var rng=new Random(); var warnings=new List<RosterWarning>(); var result=new List<DutyAssignment>(existing);
  var active=teachers.Where(t=>t.IsActive).ToList();
  foreach(var slot in slots.OrderBy(s=>s.Date).ThenBy(s=>s.StartTime)){
   for(int i=0;i<slot.TeachersRequired;i++){
    var candidate=active.Select(t=>new{T=t,S=Score(t,slot,result,unavailable,rng)}).Where(x=>x.S<int.MaxValue/4).OrderBy(x=>x.S).ThenBy(_=>rng.Next()).FirstOrDefault();
    if(candidate is null){warnings.Add(new($"Could not fully staff {slot.Date} {slot.Subject} ({slot.Venue})")); break;}
    result.Add(new DutyAssignment{TeacherId=candidate.T.Id,ExamDutySlotId=slot.Id,AssignedOn=DateTime.UtcNow});
   }
  }
  return Task.FromResult((result.Except(existing).ToList(),warnings));
 }
 int Score(Teacher t,ExamDutySlot slot,List<DutyAssignment> current,List<TeacherUnavailableSlot> unv,Random rng){
  if(slot.ShiftType==ShiftType.Morning && !t.CanWorkMorning) return int.MaxValue/2; if(slot.ShiftType==ShiftType.Afternoon && !t.CanWorkAfternoon) return int.MaxValue/2;
  var mine=current.Where(a=>a.TeacherId==t.Id).ToList();
  if(mine.Any(a=>a.ExamDutySlot is not null && a.ExamDutySlot.Date==slot.Date && a.ExamDutySlot.StartTime < slot.EndTime && slot.StartTime < a.ExamDutySlot.EndTime)) return int.MaxValue/2;
  if(unv.Any(u=>u.TeacherId==t.Id && u.Date==slot.Date && u.StartTime < slot.EndTime && slot.StartTime < u.EndTime)) return int.MaxValue/2;
  var total=mine.Sum(a=>a.ExamDutySlot?.DurationMinutes ?? 0); var shift=mine.Where(a=>a.ExamDutySlot?.ShiftType==slot.ShiftType).Sum(a=>a.ExamDutySlot?.DurationMinutes ?? 0);
  return total*3+shift*2+mine.Count*10+rng.Next(0,10);
 }
}
