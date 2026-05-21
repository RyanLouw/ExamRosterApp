namespace ExamRosterApp.Rostering;

public sealed class RosterGenerator
{
    public RosterResult GenerateRoster(List<Teacher> teachers, List<ExamDutySlot> slots)
    {
        var result = new RosterResult();
        var normalizedSlots = slots.Select(NormalizeSlotRules).ToList();
        var slotById = normalizedSlots.ToDictionary(s => s.Id);
        var stats = teachers.ToDictionary(
            t => t.Id,
            t => new TeacherDutyStats { TeacherId = t.Id });

        var orderedSlots = normalizedSlots
            .OrderByDescending(GetDifficulty)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();

        foreach (var slot in orderedSlots)
        {
            for (var i = 0; i < slot.TeachersRequired; i++)
            {
                var candidates = teachers
                    .Where(t => CanAssignTeacher(t, slot, result.Assignments, slotById))
                    .Select(t => new
                    {
                        Teacher = t,
                        Score = CalculateTeacherScore(t, slot, stats[t.Id])
                    })
                    .OrderBy(x => x.Score)
                    .ThenBy(x => x.Teacher.FullName, StringComparer.InvariantCulture)
                    .ToList();

                if (candidates.Count == 0)
                {
                    result.Warnings.Add(
                        $"Could not fully staff slot {slot.Date:yyyy-MM-dd} {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm} at {slot.Venue} ({slot.Subject}).");
                    continue;
                }

                var chosen = candidates[0].Teacher;
                result.Assignments.Add(new DutyAssignment { TeacherId = chosen.Id, ExamDutySlotId = slot.Id });
                UpdateStats(stats[chosen.Id], slot);
            }
        }

        ValidateHardConstraints(result.Assignments, teachers, slotById, result.Warnings);
        return result;
    }

    private static ExamDutySlot NormalizeSlotRules(ExamDutySlot slot)
    {
        var isLowerGrade = slot.Grade.Contains("8", StringComparison.OrdinalIgnoreCase)
            || slot.Grade.Contains("9", StringComparison.OrdinalIgnoreCase);

        var teachersRequired = isLowerGrade
            ? 1
            : slot.TeachersRequired;

        if (slot.LearnerCount is > 0 && slot.LearnersPerInvigilator is > 0)
        {
            var byCount = (int)Math.Ceiling((double)slot.LearnerCount.Value / slot.LearnersPerInvigilator.Value);
            teachersRequired = Math.Max(teachersRequired, byCount);
        }

        return new ExamDutySlot
        {
            Id = slot.Id,
            Date = slot.Date,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime,
            ShiftType = slot.ShiftType,
            Grade = slot.Grade,
            Subject = slot.Subject,
            Venue = slot.Venue,
            LearnerCount = slot.LearnerCount,
            LearnersPerInvigilator = slot.LearnersPerInvigilator,
            TeachersRequired = Math.Max(1, teachersRequired)
        };
    }

    private static int GetDifficulty(ExamDutySlot slot)
        => slot.TeachersRequired * 10 + slot.DurationMinutes;

    private static bool CanAssignTeacher(
        Teacher teacher,
        ExamDutySlot slot,
        List<DutyAssignment> currentAssignments,
        IReadOnlyDictionary<int, ExamDutySlot> allSlots)
    {
        if (slot.ShiftType == ShiftType.Morning && !teacher.CanWorkMorning)
            return false;

        if (slot.ShiftType == ShiftType.Afternoon && !teacher.CanWorkAfternoon)
            return false;

        if (teacher.Group == TeacherGroup.MainInactive)
            return false;

        if (!string.IsNullOrWhiteSpace(teacher.HomeSubject) &&
            teacher.HomeSubject.Equals(slot.Subject, StringComparison.OrdinalIgnoreCase))
            return false;

        if (teacher.MaxDutyMinutes is not null)
        {
            var assignedMinutes = currentAssignments
                .Where(a => a.TeacherId == teacher.Id)
                .Select(a => allSlots[a.ExamDutySlotId].DurationMinutes)
                .Sum();

            if (assignedMinutes + slot.DurationMinutes > teacher.MaxDutyMinutes.Value)
                return false;
        }

        var overlapsAssignment = currentAssignments
            .Where(a => a.TeacherId == teacher.Id)
            .Select(a => allSlots[a.ExamDutySlotId])
            .Any(existing => existing.Date == slot.Date && TimesOverlap(existing, slot));

        if (overlapsAssignment)
            return false;

        var unavailable = teacher.UnavailableSlots.Any(u =>
            u.Date == slot.Date &&
            u.StartTime < slot.EndTime &&
            slot.StartTime < u.EndTime);

        return !unavailable;
    }

    private static int CalculateTeacherScore(Teacher teacher, ExamDutySlot slot, TeacherDutyStats stats)
    {
        var score = stats.TotalMinutes;

        if (teacher.MinDutyMinutes is not null && stats.TotalMinutes < teacher.MinDutyMinutes.Value)
        {
            score -= 100;
            score -= (teacher.MinDutyMinutes.Value - stats.TotalMinutes) / 10;
        }

        if (teacher.Group == TeacherGroup.SecondaryNonPriority)
            score += 150;

        score += slot.ShiftType switch
        {
            ShiftType.Morning => stats.MorningMinutes * 2,
            ShiftType.Afternoon => stats.AfternoonMinutes * 2,
            _ => 0
        };

        if (stats.LastVenue is not null && stats.LastVenue.Equals(slot.Venue, StringComparison.OrdinalIgnoreCase))
            score += 25;

        if (stats.LastDate == slot.Date && stats.LastEndTime is not null)
        {
            var gap = slot.StartTime.ToTimeSpan() - stats.LastEndTime.Value.ToTimeSpan();
            if (gap.TotalMinutes >= 0 && gap.TotalMinutes < 30)
                score += 50;
        }

        // Keep people from receiving zero duties when avoidable.
        if (stats.TotalDuties == 0)
            score -= 40;

        return score;
    }

    private static void UpdateStats(TeacherDutyStats stats, ExamDutySlot slot)
    {
        stats.TotalMinutes += slot.DurationMinutes;
        stats.TotalDuties += 1;
        stats.LastVenue = slot.Venue;
        stats.LastDate = slot.Date;
        stats.LastEndTime = slot.EndTime;

        if (slot.ShiftType == ShiftType.Morning)
            stats.MorningMinutes += slot.DurationMinutes;
        else
            stats.AfternoonMinutes += slot.DurationMinutes;
    }

    private static void ValidateHardConstraints(
        List<DutyAssignment> assignments,
        List<Teacher> teachers,
        IReadOnlyDictionary<int, ExamDutySlot> slots,
        List<string> warnings)
    {
        var teachersById = teachers.ToDictionary(t => t.Id);

        foreach (var group in assignments.GroupBy(a => a.ExamDutySlotId))
        {
            var slot = slots[group.Key];
            if (group.Count() < slot.TeachersRequired)
            {
                warnings.Add($"Underfilled slot: {slot.Date:yyyy-MM-dd} {slot.Subject} at {slot.Venue}.");
            }
        }

        foreach (var teacherAssignments in assignments.GroupBy(a => a.TeacherId))
        {
            var tSlots = teacherAssignments.Select(a => slots[a.ExamDutySlotId]).OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToList();
            for (var i = 0; i < tSlots.Count; i++)
            {
                for (var j = i + 1; j < tSlots.Count; j++)
                {
                    if (tSlots[i].Date == tSlots[j].Date && TimesOverlap(tSlots[i], tSlots[j]))
                    {
                        warnings.Add($"Double booking detected for {teachersById[teacherAssignments.Key].FullName}.");
                    }
                }
            }
        }
    }

    private static bool TimesOverlap(ExamDutySlot a, ExamDutySlot b)
        => a.StartTime < b.EndTime && b.StartTime < a.EndTime;
}
