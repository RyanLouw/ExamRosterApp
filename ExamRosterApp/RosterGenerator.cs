namespace ExamRosterApp.Rostering;

public sealed class RosterGenerator
{
    public RosterResult GenerateRoster(List<Teacher> teachers, List<ExamDutySlot> slots)
    {
        var multiRunResult = GenerateBestOfMultipleRuns(teachers, slots);
        multiRunResult.Diagnostics.Insert(0, $"Multi-run mode: tried 20 variants, picked best spread={multiRunResult.FairnessSpreadMinutes} minutes.");
        return multiRunResult;
    }

    private RosterResult GenerateBestOfMultipleRuns(List<Teacher> teachers, List<ExamDutySlot> slots)
    {
        RosterResult? best = null;
        var attempts = 20;
        var runSummaries = new List<string>();

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            var result = GenerateSingleRun(teachers, slots, attempt);
            runSummaries.Add($"attempt={attempt}, spread={result.FairnessSpreadMinutes}, warnings={result.Warnings.Count}, assignments={result.Assignments.Count}");
            if (best is null
                || result.FairnessSpreadMinutes < best.FairnessSpreadMinutes
                || (result.FairnessSpreadMinutes == best.FairnessSpreadMinutes && result.Warnings.Count < best.Warnings.Count))
            {
                best = result;
            }
        }

        if (best is not null)
        {
            var spreads = runSummaries
                .Select(s => s.Split(',', StringSplitOptions.TrimEntries)[1].Split('=')[1])
                .Distinct()
                .Count();
            best.Diagnostics.Insert(0, "=== Multi-run attempt summary ===");
            for (var i = 0; i < runSummaries.Count; i++)
                best.Diagnostics.Insert(1 + i, runSummaries[i]);
            best.Diagnostics.Insert(1 + runSummaries.Count, $"distinctSpreadValues={spreads}");
        }

        return best ?? new RosterResult();
    }

    private RosterResult GenerateSingleRun(List<Teacher> teachers, List<ExamDutySlot> slots, int seed)
    {
        var seededTeachers = teachers
            .OrderBy(t => StableHash($"{seed}-{t.Id}-{t.FullName}"))
            .ToList();

        var optimizedSlots = OptimizeSeniorSlotStaffing(teachers, slots);
        var result = new RosterResult();
        var normalizedSlots = new List<ExamDutySlot>(optimizedSlots.Count);
        foreach (var slot in optimizedSlots)
        {
            var normalized = NormalizeSlotRules(slot);
            normalizedSlots.Add(normalized);

            if (normalized.TeachersRequired != slot.TeachersRequired)
            {
                result.Diagnostics.Add(
                    $"Normalized slot {slot.Id}: teachersRequired {slot.TeachersRequired} -> {normalized.TeachersRequired} " +
                    $"(grade={slot.Grade}, intervalCount={slot.LearnerCount?.ToString() ?? "null"}, teachersPerInterval={slot.LearnersPerInvigilator?.ToString() ?? "null"}).");
            }
        }

        var slotById = normalizedSlots.ToDictionary(s => s.Id);
        var stats = seededTeachers.ToDictionary(
            t => t.Id,
            t => new TeacherDutyStats { TeacherId = t.Id });

        var orderedSlots = normalizedSlots
            .OrderByDescending(GetDifficulty)
            .ThenBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ThenBy(s => StableHash($"{seed}-{s.Id}-{s.Subject}"))
            .ToList();

        foreach (var slot in orderedSlots)
        {
            result.Diagnostics.Add($"Slot {slot.Id} {slot.Date:yyyy-MM-dd} {slot.Subject}: teachersRequired={slot.TeachersRequired}, duration={slot.DurationMinutes}m");
            for (var i = 0; i < slot.TeachersRequired; i++)
            {
                var candidates = teachers
                    .Where(t => CanAssignTeacher(t, slot, result.Assignments, slotById))
                    .Select(t => new
                    {
                        Teacher = t,
                        Score = CalculateTeacherScore(t, slot, stats[t.Id]),
                        ProjectedMinutes = stats[t.Id].TotalMinutes + slot.DurationMinutes,
                        ProjectedDuties = stats[t.Id].TotalDuties + 1
                    })
                    .OrderBy(x => x.ProjectedMinutes)
                    .ThenBy(x => x.ProjectedDuties)
                    .ThenBy(x => x.Score)
                    .ThenBy(x => StableHash($"{seed}-{slot.Id}-{x.Teacher.Id}"))
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
                result.Diagnostics.Add($"Assigned teacher {chosen.Id} ({chosen.FullName}) to slot {slot.Id}");
            }
        }

        RebalanceForFairness(result.Assignments, teachers, slotById);
        ValidateHardConstraints(result.Assignments, teachers, slotById, result.Warnings);
        var totals = teachers.ToDictionary(
            t => t.Id,
            t => result.Assignments.Where(a => a.TeacherId == t.Id).Select(a => slotById[a.ExamDutySlotId].DurationMinutes).Sum());
        var totalAssignedMinutes = totals.Values.Sum();
        var avgMinutes = teachers.Count == 0 ? 0 : totalAssignedMinutes / (double)teachers.Count;
        var maxMinutes = totals.Values.DefaultIfEmpty(0).Max();
        var minMinutes = totals.Values.DefaultIfEmpty(0).Min();
        result.FairnessSpreadMinutes = maxMinutes - minMinutes;
        result.Diagnostics.Add(
            $"Fairness math: totalAssignedMinutes={totalAssignedMinutes}, teachers={teachers.Count}, " +
            $"averageMinutes={avgMinutes:F2}, minMinutes={minMinutes}, maxMinutes={maxMinutes}, diff={maxMinutes - minMinutes}.");
        var minPossibleSpread = normalizedSlots.Max(s => s.DurationMinutes);
        result.Diagnostics.Add($"Fairness lower bound estimate: at least {minPossibleSpread} minutes spread when some teachers can remain unassigned.");
        result.Diagnostics.Add($"Final fairness spread minutes: min={totals.Values.Min()}, max={totals.Values.Max()}, diff={totals.Values.Max() - totals.Values.Min()}");
        result.Diagnostics.Add("=== Teacher totals (minutes) ===");
        foreach (var kv in totals.OrderBy(k => k.Key))
            result.Diagnostics.Add($"teacherId={kv.Key}, totalMinutes={kv.Value}");
        return result;
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            var hash = 23;
            foreach (var c in text)
                hash = (hash * 31) + c;
            return hash;
        }
    }

    private static List<ExamDutySlot> OptimizeSeniorSlotStaffing(List<Teacher> teachers, List<ExamDutySlot> slots)
    {
        var working = slots.Select(s => s).ToList();
        var seniorIndexes = working
            .Select((slot, idx) => new { slot, idx })
            .Where(x => x.slot.Grade.Contains("10") || x.slot.Grade.Contains("11") || x.slot.Grade.Contains("12"))
            .Select(x => x.idx)
            .ToList();

        if (seniorIndexes.Count == 0) return working;

        var target = EstimateTargetMinutes(teachers, working);
        foreach (var i in seniorIndexes)
        {
            var slot = working[i];
            var minT = Math.Max(1, slot.MinTeachersRequired ?? slot.TeachersRequired);
            var maxT = Math.Max(minT, slot.MaxTeachersRequired ?? slot.TeachersRequired);

            var best = slot.TeachersRequired;
            var bestScore = double.MaxValue;
            for (var t = minT; t <= maxT; t++)
            {
                var projected = (double)t * slot.DurationMinutes;
                var score = Math.Abs(projected - target);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            working[i] = new ExamDutySlot
            {
                Id = slot.Id, Date = slot.Date, StartTime = slot.StartTime, EndTime = slot.EndTime, ShiftType = slot.ShiftType,
                Grade = slot.Grade, Subject = slot.Subject, Venue = slot.Venue,
                TeachersRequired = best, MinTeachersRequired = slot.MinTeachersRequired, MaxTeachersRequired = slot.MaxTeachersRequired,
                LearnerCount = slot.LearnerCount, LearnersPerInvigilator = slot.LearnersPerInvigilator
            };
        }

        return working;
    }

    private static double EstimateTargetMinutes(List<Teacher> teachers, List<ExamDutySlot> slots)
    {
        var total = slots.Sum(s => Math.Max(1, s.TeachersRequired) * s.DurationMinutes);
        return teachers.Count == 0 ? 0 : (double)total / teachers.Count;
    }

    private static ExamDutySlot NormalizeSlotRules(ExamDutySlot slot)
    {
        var isLowerGrade = slot.Grade.Contains("8", StringComparison.OrdinalIgnoreCase)
            || slot.Grade.Contains("9", StringComparison.OrdinalIgnoreCase);

        var teachersRequired = isLowerGrade
            ? 1
            : slot.TeachersRequired;

        if (!isLowerGrade && slot.MaxTeachersRequired is not null)
            teachersRequired = Math.Min(teachersRequired, slot.MaxTeachersRequired.Value);
        if (!isLowerGrade && slot.MinTeachersRequired is not null)
            teachersRequired = Math.Max(teachersRequired, slot.MinTeachersRequired.Value);

        var intervalCount = slot.LearnerCount;
        if (!isLowerGrade && intervalCount is null or <= 0)
            intervalCount = Math.Max(1, (int)Math.Ceiling(slot.DurationMinutes / 120.0)); // max 2h per interval

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
            LearnerCount = isLowerGrade ? 1 : intervalCount,
            LearnersPerInvigilator = null,
            MinTeachersRequired = slot.MinTeachersRequired,
            MaxTeachersRequired = slot.MaxTeachersRequired,
            TeachersRequired = isLowerGrade ? 1 : Math.Max(1, teachersRequired)
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
        var score = stats.TotalMinutes * 3;

        if (teacher.MinDutyMinutes is not null && stats.TotalMinutes < teacher.MinDutyMinutes.Value)
            score -= 300;

        if (teacher.Group == TeacherGroup.SecondaryNonPriority)
            score += 150;

        score += slot.ShiftType switch
        {
            ShiftType.Morning => stats.MorningMinutes,
            ShiftType.Afternoon => stats.AfternoonMinutes,
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

        if (stats.TotalDuties == 0)
            score -= 100;

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

    private static void RebalanceForFairness(
        List<DutyAssignment> assignments,
        List<Teacher> teachers,
        IReadOnlyDictionary<int, ExamDutySlot> slots)
    {
        for (var pass = 0; pass < 40; pass++)
        {
            var totals = teachers.ToDictionary(t => t.Id, t => assignments
                .Where(a => a.TeacherId == t.Id)
                .Select(a => slots[a.ExamDutySlotId].DurationMinutes)
                .Sum());

            var maxMinutes = totals.Values.Max();
            var minMinutes = totals.Values.Min();
            if (maxMinutes - minMinutes <= 60)
                return;

            var improved = TryBestFairnessMove(assignments, teachers, slots, totals);
            if (!improved)
                return;
        }
    }

    private static bool TryBestFairnessMove(
        List<DutyAssignment> assignments,
        List<Teacher> teachers,
        IReadOnlyDictionary<int, ExamDutySlot> slots,
        IReadOnlyDictionary<int, int> totals)
    {
        var baseSpread = totals.Values.Max() - totals.Values.Min();
        DutyAssignment? bestAssignment = null;
        Teacher? bestTargetTeacher = null;
        var bestSpread = baseSpread;

        var overloadedTeachers = teachers
            .OrderByDescending(t => totals[t.Id])
            .Take(12)
            .ToList();

        var underloadedTeachers = teachers
            .OrderBy(t => totals[t.Id])
            .Take(24)
            .ToList();

        foreach (var sourceTeacher in overloadedTeachers)
        {
            var sourceAssignments = assignments
                .Where(a => a.TeacherId == sourceTeacher.Id)
                .Select(a => new { Assignment = a, Slot = slots[a.ExamDutySlotId] })
                .OrderByDescending(x => x.Slot.DurationMinutes)
                .ToList();

            foreach (var item in sourceAssignments)
            {
                foreach (var targetTeacher in underloadedTeachers)
                {
                    if (targetTeacher.Id == sourceTeacher.Id)
                        continue;

                    if (!CanSwapToTeacher(targetTeacher, item.Slot, assignments, slots, item.Assignment))
                        continue;

                    var proposed = totals.ToDictionary(k => k.Key, v => v.Value);
                    proposed[sourceTeacher.Id] -= item.Slot.DurationMinutes;
                    proposed[targetTeacher.Id] += item.Slot.DurationMinutes;

                    var proposedSpread = proposed.Values.Max() - proposed.Values.Min();
                    if (proposedSpread < bestSpread)
                    {
                        bestSpread = proposedSpread;
                        bestAssignment = item.Assignment;
                        bestTargetTeacher = targetTeacher;
                    }
                }
            }
        }

        if (bestAssignment is null || bestTargetTeacher is null || bestSpread >= baseSpread)
            return false;

        assignments.Remove(bestAssignment);
        assignments.Add(new DutyAssignment { TeacherId = bestTargetTeacher.Id, ExamDutySlotId = bestAssignment.ExamDutySlotId });
        return true;
    }

    private static bool CanSwapToTeacher(
        Teacher teacher,
        ExamDutySlot slot,
        List<DutyAssignment> currentAssignments,
        IReadOnlyDictionary<int, ExamDutySlot> allSlots,
        DutyAssignment assignmentToReplace)
    {
        var reduced = currentAssignments.Where(a => !ReferenceEquals(a, assignmentToReplace)).ToList();
        return CanAssignTeacher(teacher, slot, reduced, allSlots);
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
