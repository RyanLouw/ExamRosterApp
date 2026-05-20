using System.Globalization;
using System.Text;
using ExamRosterApp.Rostering;
using System.Windows.Forms;

namespace ExamRosterApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new RosterForm());
    }
}

public sealed class RosterForm : Form
{
    private readonly TextBox _teachersBox = new() { Multiline = true, ScrollBars = ScrollBars.Both, Width = 560, Height = 180 };
    private readonly TextBox _slotsBox = new() { Multiline = true, ScrollBars = ScrollBars.Both, Width = 560, Height = 180 };
    private readonly Button _generateButton = new() { Text = "Generate roster", Width = 160, Height = 36 };
    private readonly TextBox _outputBox = new() { Multiline = true, ScrollBars = ScrollBars.Both, Width = 560, Height = 220, ReadOnly = true };

    public RosterForm()
    {
        Text = "Exam Duty Scheduler";
        Width = 620;
        Height = 730;

        var layout = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12)
        };

        layout.Controls.Add(new Label { Text = "Teachers CSV: Id,FullName,CanWorkMorning,CanWorkAfternoon,HomeSubject,Unavailable", AutoSize = true });
        layout.Controls.Add(new Label { Text = "Unavailable format: yyyy-MM-dd|HH:mm-HH:mm;yyyy-MM-dd|HH:mm-HH:mm", AutoSize = true });
        layout.Controls.Add(_teachersBox);

        layout.Controls.Add(new Label { Text = "Exam Slots CSV: Id,Date,Start,End,Shift(Morning/Afternoon),Grade,Subject,Venue,TeachersRequired", AutoSize = true, Padding = new Padding(0, 10, 0, 0) });
        layout.Controls.Add(_slotsBox);

        _generateButton.Click += (_, _) => GenerateRoster();
        layout.Controls.Add(_generateButton);

        layout.Controls.Add(new Label { Text = "Generated roster", AutoSize = true, Padding = new Padding(0, 10, 0, 0) });
        layout.Controls.Add(_outputBox);

        Controls.Add(layout);

        SeedSampleData();
    }

    private void SeedSampleData()
    {
        _teachersBox.Text = string.Join(Environment.NewLine,
            "1,Mrs Smith,true,true,Maths,2026-06-01|10:00-11:00",
            "2,Mr Jones,true,false,,-",
            "3,Mrs Botha,false,true,,-",
            "4,Ms Patel,true,true,,-");

        _slotsBox.Text = string.Join(Environment.NewLine,
            "1,2026-06-01,08:00,10:00,Morning,Grade 8,Maths,Hall A,2",
            "2,2026-06-01,08:00,10:00,Morning,Grade 9,English,Room 12,1",
            "3,2026-06-01,13:00,15:00,Afternoon,Grade 10,Science,Hall B,2");
    }

    private void GenerateRoster()
    {
        try
        {
            var teachers = ParseTeachers(_teachersBox.Text);
            var slots = ParseSlots(_slotsBox.Text);

            var generator = new RosterGenerator();
            var result = generator.GenerateRoster(teachers, slots);

            var slotById = slots.ToDictionary(s => s.Id);
            var teacherById = teachers.ToDictionary(t => t.Id);

            var sb = new StringBuilder();
            sb.AppendLine("Date       Time        Shift      Venue      Teacher");
            sb.AppendLine("------------------------------------------------------");

            foreach (var assignment in result.Assignments
                         .OrderBy(a => slotById[a.ExamDutySlotId].Date)
                         .ThenBy(a => slotById[a.ExamDutySlotId].StartTime)
                         .ThenBy(a => slotById[a.ExamDutySlotId].Venue))
            {
                var slot = slotById[assignment.ExamDutySlotId];
                var teacher = teacherById[assignment.TeacherId];
                sb.AppendLine($"{slot.Date:yyyy-MM-dd} {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm} {slot.ShiftType,-10} {slot.Venue,-10} {teacher.FullName}");
            }

            if (result.Warnings.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Warnings:");
                foreach (var warning in result.Warnings)
                {
                    sb.AppendLine($"- {warning}");
                }
            }

            _outputBox.Text = sb.ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Input error: {ex.Message}", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static List<Teacher> ParseTeachers(string csv)
    {
        var teachers = new List<Teacher>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            var p = line.Split(',', StringSplitOptions.TrimEntries);
            if (p.Length < 6) throw new InvalidOperationException($"Teacher row needs 6 columns: {line}");

            teachers.Add(new Teacher
            {
                Id = int.Parse(p[0], CultureInfo.InvariantCulture),
                FullName = p[1],
                CanWorkMorning = bool.Parse(p[2]),
                CanWorkAfternoon = bool.Parse(p[3]),
                HomeSubject = string.IsNullOrWhiteSpace(p[4]) ? null : p[4],
                UnavailableSlots = ParseUnavailable(p[5])
            });
        }

        return teachers;
    }

    private static List<UnavailableSlot> ParseUnavailable(string value)
    {
        var list = new List<UnavailableSlot>();
        if (string.IsNullOrWhiteSpace(value) || value == "-") return list;

        foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var pair = part.Split('|', StringSplitOptions.TrimEntries);
            if (pair.Length != 2) throw new InvalidOperationException($"Invalid unavailable slot: {part}");

            var times = pair[1].Split('-', StringSplitOptions.TrimEntries);
            if (times.Length != 2) throw new InvalidOperationException($"Invalid unavailable times: {pair[1]}");

            list.Add(new UnavailableSlot
            {
                Date = DateOnly.Parse(pair[0], CultureInfo.InvariantCulture),
                StartTime = TimeOnly.Parse(times[0], CultureInfo.InvariantCulture),
                EndTime = TimeOnly.Parse(times[1], CultureInfo.InvariantCulture)
            });
        }

        return list;
    }

    private static List<ExamDutySlot> ParseSlots(string csv)
    {
        var slots = new List<ExamDutySlot>();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var p = line.Split(',', StringSplitOptions.TrimEntries);
            if (p.Length < 9) throw new InvalidOperationException($"Exam slot row needs 9 columns: {line}");

            slots.Add(new ExamDutySlot
            {
                Id = int.Parse(p[0], CultureInfo.InvariantCulture),
                Date = DateOnly.Parse(p[1], CultureInfo.InvariantCulture),
                StartTime = TimeOnly.Parse(p[2], CultureInfo.InvariantCulture),
                EndTime = TimeOnly.Parse(p[3], CultureInfo.InvariantCulture),
                ShiftType = Enum.Parse<ShiftType>(p[4], true),
                Grade = p[5],
                Subject = p[6],
                Venue = p[7],
                TeachersRequired = int.Parse(p[8], CultureInfo.InvariantCulture)
            });
        }

        return slots;
    }
}
