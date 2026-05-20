using ExamRosterApp.Rostering;

var teachers = new List<Teacher>
{
    new()
    {
        Id = 1,
        FullName = "Mrs Smith",
        CanWorkMorning = true,
        CanWorkAfternoon = true,
        HomeSubject = "Maths",
        UnavailableSlots =
        [
            new UnavailableSlot
            {
                Date = new DateOnly(2026, 6, 1),
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(11, 0)
            }
        ]
    },
    new()
    {
        Id = 2,
        FullName = "Mr Jones",
        CanWorkMorning = true,
        CanWorkAfternoon = false
    },
    new()
    {
        Id = 3,
        FullName = "Mrs Botha",
        CanWorkMorning = false,
        CanWorkAfternoon = true
    },
    new()
    {
        Id = 4,
        FullName = "Ms Patel",
        CanWorkMorning = true,
        CanWorkAfternoon = true
    }
};

var slots = new List<ExamDutySlot>
{
    new()
    {
        Id = 1,
        Date = new DateOnly(2026, 6, 1),
        ShiftType = ShiftType.Morning,
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(10, 0),
        Grade = "Grade 8",
        Subject = "Maths",
        Venue = "Hall A",
        TeachersRequired = 2
    },
    new()
    {
        Id = 2,
        Date = new DateOnly(2026, 6, 1),
        ShiftType = ShiftType.Morning,
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(10, 0),
        Grade = "Grade 9",
        Subject = "English",
        Venue = "Room 12",
        TeachersRequired = 1
    },
    new()
    {
        Id = 3,
        Date = new DateOnly(2026, 6, 1),
        ShiftType = ShiftType.Afternoon,
        StartTime = new TimeOnly(13, 0),
        EndTime = new TimeOnly(15, 0),
        Grade = "Grade 10",
        Subject = "Science",
        Venue = "Hall B",
        TeachersRequired = 2
    }
};

var generator = new RosterGenerator();
var result = generator.GenerateRoster(teachers, slots);

var slotById = slots.ToDictionary(s => s.Id);
var teacherById = teachers.ToDictionary(t => t.Id);

Console.WriteLine("Generated Exam Duty Roster");
Console.WriteLine("Date       Time        Shift      Venue      Teacher");
Console.WriteLine("------------------------------------------------------");

foreach (var assignment in result.Assignments
             .OrderBy(a => slotById[a.ExamDutySlotId].Date)
             .ThenBy(a => slotById[a.ExamDutySlotId].StartTime)
             .ThenBy(a => slotById[a.ExamDutySlotId].Venue)
             .ThenBy(a => teacherById[a.TeacherId].FullName))
{
    var slot = slotById[assignment.ExamDutySlotId];
    var teacher = teacherById[assignment.TeacherId];
    Console.WriteLine($"{slot.Date:yyyy-MM-dd} {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm} {slot.ShiftType,-10} {slot.Venue,-10} {teacher.FullName}");
}

if (result.Warnings.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Warnings:");
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"- {warning}");
    }
}
