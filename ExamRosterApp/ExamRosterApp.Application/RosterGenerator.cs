using ExamRosterApp.Domain;
namespace ExamRosterApp.Application;
public class RosterGenerator : IRosterGenerator {
 public Task<RosterResult> GenerateRosterAsync(IEnumerable<Teacher> teachers, IEnumerable<ExamDutySlot> slots, IEnumerable<TeacherUnavailableSlot> unavail, IEnumerable<DutyAssignment> existing){
  var t=teachers.Where(x=>x.IsActive).ToList(); var s=slots.OrderBy(x=>x.Date).ThenBy(x=>x.StartTime).ToList(); var u=unavail.ToList(); var a=existing.ToList(); var warnings=new List<string>();
  var totals=t.ToDictionary(x=>x.Id,_=>0); var morn=t.ToDictionary(x=>x.Id,_=>0); var aft=t.ToDictionary(x=>x.Id,_=>0); var duties=t.ToDictionary(x=>x.Id,_=>0);
  foreach(var slot in s){ for(int i=0;i<slot.TeachersRequired;i++){ var cand=t.Where(tt=>Valid(tt,slot,a,s,u)).OrderBy(tt=>Score(tt,slot,totals,morn,aft,duties)).FirstOrDefault(); if(cand is null){warnings.Add($"Unable to fully staff {slot.Date} {slot.Subject} at {slot.Venue}."); continue;} var da=new DutyAssignment{TeacherId=cand.Id,ExamDutySlotId=slot.Id}; a.Add(da); totals[cand.Id]+=slot.DurationMinutes; if(slot.ShiftType==ShiftType.Morning)morn[cand.Id]+=slot.DurationMinutes; else aft[cand.Id]+=slot.DurationMinutes; duties[cand.Id]++; }}
  return Task.FromResult(new RosterResult(a.Except(existing).ToList(),warnings)); }
 static bool Valid(Teacher t, ExamDutySlot slot, List<DutyAssignment> asg, List<ExamDutySlot> slots, List<TeacherUnavailableSlot> un){ if(slot.ShiftType==ShiftType.Morning&&!t.CanWorkMorning) return false; if(slot.ShiftType==ShiftType.Afternoon&&!t.CanWorkAfternoon) return false; if(un.Any(u=>u.TeacherId==t.Id&&u.Date==slot.Date&&Overlaps(u.StartTime,u.EndTime,slot.StartTime,slot.EndTime))) return false; foreach(var a in asg.Where(x=>x.TeacherId==t.Id)){ var other=slots.FirstOrDefault(x=>x.Id==a.ExamDutySlotId); if(other!=null&&other.Date==slot.Date&&Overlaps(other.StartTime,other.EndTime,slot.StartTime,slot.EndTime)) return false;} return true; }
 static bool Overlaps(TimeOnly a1,TimeOnly a2,TimeOnly b1,TimeOnly b2)=>a1<b2&&b1<a2;
 static double Score(Teacher t,ExamDutySlot s,Dictionary<int,int> total,Dictionary<int,int> m,Dictionary<int,int> a,Dictionary<int,int> d)=>total[t.Id]+(s.ShiftType==ShiftType.Morning?m[t.Id]:a[t.Id])+d[t.Id]*5+Random.Shared.NextDouble();
}
