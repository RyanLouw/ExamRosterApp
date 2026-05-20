using ExamRosterApp.Domain;
using ExamRosterApp.Engine;

var teachers = new List<Teacher>
{
    new()
    {
        Id = 1,
        FullName = "Mrs Smith",
        CanWorkMorning = true,
        CanWorkAfternoon = true,
        MaxDutiesPerDay = 2
    },
    new()
    {
        Id = 2,
        FullName = "Mr Jones",
        CanWorkMorning = true,
        CanWorkAfternoon = false,
        MaxDutiesPerDay = 2
    },
    new()
    {
        Id = 3,
        FullName = "Mrs Botha",
        CanWorkMorning = false,
        CanWorkAfternoon = true,
        MaxDutiesPerDay = 2,
        UnavailableSlots =
        [
            new UnavailableSlot
            {
                Date = new DateOnly(2026, 6, 2),
                StartTime = new TimeOnly(13, 0),
                EndTime = new TimeOnly(15, 0)
            }
        ]
    },
    new()
    {
        Id = 4,
        FullName = "Ms Daniels",
        CanWorkMorning = true,
        CanWorkAfternoon = true,
        MaxDutiesPerDay = 3
    }
};

var slots = new List<ExamDutySlot>
{
    new()
    {
        Id = 101,
        Date = new DateOnly(2026, 6, 1),
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(10, 0),
        ShiftType = ShiftType.Morning,
        Grade = "Grade 8",
        Subject = "Maths",
        Venue = "Hall A",
        TeachersRequired = 2
    },
    new()
    {
        Id = 102,
        Date = new DateOnly(2026, 6, 1),
        StartTime = new TimeOnly(8, 0),
        EndTime = new TimeOnly(10, 0),
        ShiftType = ShiftType.Morning,
        Grade = "Grade 9",
        Subject = "English",
        Venue = "Room 12",
        TeachersRequired = 1
    },
    new()
    {
        Id = 103,
        Date = new DateOnly(2026, 6, 1),
        StartTime = new TimeOnly(13, 0),
        EndTime = new TimeOnly(15, 0),
        ShiftType = ShiftType.Afternoon,
        Grade = "Grade 10",
        Subject = "Science",
        Venue = "Hall B",
        TeachersRequired = 2
    }
};

var generator = new RosterGenerator();
var assignments = generator.GenerateRoster(teachers, slots);

var teacherLookup = teachers.ToDictionary(t => t.Id, t => t.FullName);
var slotLookup = slots.ToDictionary(s => s.Id);

Console.WriteLine("Generated Exam Duty Roster");
Console.WriteLine("===========================");

foreach (var assignment in assignments.OrderBy(a => slotLookup[a.ExamDutySlotId].Date)
                                      .ThenBy(a => slotLookup[a.ExamDutySlotId].StartTime)
                                      .ThenBy(a => slotLookup[a.ExamDutySlotId].Venue)
                                      .ThenBy(a => teacherLookup[a.TeacherId]))
{
    var slot = slotLookup[assignment.ExamDutySlotId];
    Console.WriteLine($"{slot.Date:yyyy-MM-dd} | {slot.StartTime:HH\\:mm}-{slot.EndTime:HH\\:mm} | {slot.Venue,-8} | {teacherLookup[assignment.TeacherId]}");
}
