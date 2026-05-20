using ExamRosterApp.Domain;

namespace ExamRosterApp.Engine;

public sealed class RosterGenerator
{
    public IReadOnlyList<DutyAssignment> GenerateRoster(
        IReadOnlyList<Teacher> teachers,
        IReadOnlyList<ExamDutySlot> slots)
    {
        var assignments = new List<DutyAssignment>();
        var teacherStats = teachers.ToDictionary(
            t => t.Id,
            t => new TeacherDutyStats { TeacherId = t.Id });

        var orderedSlots = slots
            .OrderByDescending(s => s.TeachersRequired)
            .ThenByDescending(s => teachers.Count(t => IsTeacherCompatibleByAvailability(t, s)))
            .ThenBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        foreach (var slot in orderedSlots)
        {
            for (var i = 0; i < slot.TeachersRequired; i++)
            {
                var availableTeachers = teachers
                    .Where(t => CanAssignTeacher(t, slot, assignments, slots))
                    .Select(t => new
                    {
                        Teacher = t,
                        Score = CalculateTeacherScore(t, slot, assignments, slots, teacherStats[t.Id])
                    })
                    .OrderBy(x => x.Score)
                    .ThenBy(x => x.Teacher.FullName)
                    .ToList();

                if (availableTeachers.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Could not fill duty slot {slot.Id}: {slot.Date} {slot.StartTime}-{slot.EndTime} at {slot.Venue}.");
                }

                var selectedTeacher = availableTeachers[0].Teacher;

                assignments.Add(new DutyAssignment
                {
                    TeacherId = selectedTeacher.Id,
                    ExamDutySlotId = slot.Id
                });

                UpdateStats(teacherStats[selectedTeacher.Id], slot);
            }
        }

        return assignments;
    }

    private static bool CanAssignTeacher(
        Teacher teacher,
        ExamDutySlot slot,
        IReadOnlyList<DutyAssignment> currentAssignments,
        IReadOnlyList<ExamDutySlot> allSlots)
    {
        if (!IsTeacherCompatibleByAvailability(teacher, slot))
        {
            return false;
        }

        var teacherSlots = currentAssignments
            .Where(a => a.TeacherId == teacher.Id)
            .Select(a => allSlots.First(s => s.Id == a.ExamDutySlotId))
            .ToList();

        if (teacherSlots.Any(existing => existing.Date == slot.Date && TimesOverlap(existing, slot)))
        {
            return false;
        }

        var dailyAssignments = teacherSlots.Count(existing => existing.Date == slot.Date);
        if (dailyAssignments >= teacher.MaxDutiesPerDay)
        {
            return false;
        }

        var unavailable = teacher.UnavailableSlots.Any(u =>
            u.Date == slot.Date && u.StartTime < slot.EndTime && slot.StartTime < u.EndTime);

        return !unavailable;
    }

    private static bool IsTeacherCompatibleByAvailability(Teacher teacher, ExamDutySlot slot)
    {
        if (slot.ShiftType == ShiftType.Morning && !teacher.CanWorkMorning)
        {
            return false;
        }

        if (slot.ShiftType == ShiftType.Afternoon && !teacher.CanWorkAfternoon)
        {
            return false;
        }

        return true;
    }

    private static int CalculateTeacherScore(
        Teacher teacher,
        ExamDutySlot slot,
        IReadOnlyList<DutyAssignment> currentAssignments,
        IReadOnlyList<ExamDutySlot> allSlots,
        TeacherDutyStats stats)
    {
        var score = stats.TotalMinutes;

        if (slot.ShiftType == ShiftType.Morning)
        {
            score += stats.MorningMinutes * 2;
        }

        if (slot.ShiftType == ShiftType.Afternoon)
        {
            score += stats.AfternoonMinutes * 2;
        }

        var priorTeacherSlots = currentAssignments
            .Where(a => a.TeacherId == teacher.Id)
            .Select(a => allSlots.First(s => s.Id == a.ExamDutySlotId))
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        var previousSlot = priorTeacherSlots.LastOrDefault();
        if (previousSlot is not null && previousSlot.Date == slot.Date)
        {
            var gapMinutes = (slot.StartTime.ToTimeSpan() - previousSlot.EndTime.ToTimeSpan()).TotalMinutes;
            if (gapMinutes >= 0 && gapMinutes <= 30)
            {
                score += 75;
            }
        }

        var sameVenueAssignments = priorTeacherSlots.Count(s => s.Venue == slot.Venue);
        score += sameVenueAssignments * 10;

        return score;
    }

    private static void UpdateStats(TeacherDutyStats stats, ExamDutySlot slot)
    {
        stats.TotalMinutes += slot.DurationMinutes;
        stats.DutyCount += 1;

        if (slot.ShiftType == ShiftType.Morning)
        {
            stats.MorningMinutes += slot.DurationMinutes;
        }
        else if (slot.ShiftType == ShiftType.Afternoon)
        {
            stats.AfternoonMinutes += slot.DurationMinutes;
        }
    }

    private static bool TimesOverlap(ExamDutySlot a, ExamDutySlot b) =>
        a.StartTime < b.EndTime && b.StartTime < a.EndTime;
}
