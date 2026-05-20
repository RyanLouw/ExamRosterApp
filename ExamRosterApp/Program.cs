using System.Text;
using ExamRosterApp.Rostering;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Content(BuildPage(), "text/html"));

app.MapPost("/generate", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    var teachersText = form["teachers"].ToString();
    var slotsText = form["slots"].ToString();

    var teachers = ParseTeachers(teachersText);
    var slots = ParseSlots(slotsText);

    var generator = new RosterGenerator();
    var result = generator.GenerateRoster(teachers, slots);

    return Results.Content(BuildPage(teachersText, slotsText, result, teachers, slots), "text/html");
});

app.Run();

static string BuildPage(string teachers = "", string slots = "", RosterResult? result = null, List<Teacher>? teacherList = null, List<ExamDutySlot>? slotList = null)
{
    var output = new StringBuilder();
    output.Append("""
<!doctype html>
<html>
<head><meta charset='utf-8'><title>Exam Roster</title>
<style>body{font-family:Arial;max-width:1100px;margin:20px auto;padding:0 12px}textarea{width:100%;height:180px}table{border-collapse:collapse;width:100%;margin-top:12px}th,td{border:1px solid #ddd;padding:8px}th{background:#f4f4f4}</style>
</head>
<body>
<h1>Exam Duty Scheduler</h1>
<p>Enter CSV rows.</p>
<form method='post' action='/generate'>
<h3>Teachers CSV</h3>
<p><code>Id,FullName,CanWorkMorning,CanWorkAfternoon,HomeSubject,Unavailable</code> where Unavailable uses <code>yyyy-MM-dd|HH:mm-HH:mm;...</code></p>
<textarea name='teachers'>"""
    );
    output.Append(System.Net.WebUtility.HtmlEncode(teachers));
    output.Append("</textarea><h3>Exam Slots CSV</h3><p><code>Id,Date,Start,End,Shift(Morning/Afternoon),Grade,Subject,Venue,TeachersRequired</code></p><textarea name='slots'>");
    output.Append(System.Net.WebUtility.HtmlEncode(slots));
    output.Append("</textarea><br/><br/><button type='submit'>Generate roster</button></form>");

    if (result is not null && teacherList is not null && slotList is not null)
    {
        var teacherById = teacherList.ToDictionary(t => t.Id);
        var slotById = slotList.ToDictionary(s => s.Id);

        output.Append("<h2>Generated Roster</h2><table><tr><th>Date</th><th>Time</th><th>Shift</th><th>Venue</th><th>Teacher</th></tr>");
        foreach (var assignment in result.Assignments.OrderBy(a => slotById[a.ExamDutySlotId].Date).ThenBy(a => slotById[a.ExamDutySlotId].StartTime))
        {
            var slot = slotById[assignment.ExamDutySlotId];
            var teacher = teacherById[assignment.TeacherId];
            output.Append($"<tr><td>{slot.Date:yyyy-MM-dd}</td><td>{slot.StartTime:HH:mm}-{slot.EndTime:HH:mm}</td><td>{slot.ShiftType}</td><td>{slot.Venue}</td><td>{teacher.FullName}</td></tr>");
        }
        output.Append("</table>");

        if (result.Warnings.Count > 0)
        {
            output.Append("<h3>Warnings</h3><ul>");
            foreach (var warning in result.Warnings)
            {
                output.Append($"<li>{System.Net.WebUtility.HtmlEncode(warning)}</li>");
            }
            output.Append("</ul>");
        }
    }

    output.Append("</body></html>");
    return output.ToString();
}

static List<Teacher> ParseTeachers(string csv)
{
    var teachers = new List<Teacher>();
    var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var line in lines)
    {
        var p = line.Split(',', StringSplitOptions.TrimEntries);
        if (p.Length < 6 || !int.TryParse(p[0], out var id)) continue;
        teachers.Add(new Teacher
        {
            Id = id,
            FullName = p[1],
            CanWorkMorning = bool.Parse(p[2]),
            CanWorkAfternoon = bool.Parse(p[3]),
            HomeSubject = string.IsNullOrWhiteSpace(p[4]) ? null : p[4],
            UnavailableSlots = ParseUnavailable(p[5])
        });
    }
    return teachers;
}

static List<UnavailableSlot> ParseUnavailable(string value)
{
    var list = new List<UnavailableSlot>();
    if (string.IsNullOrWhiteSpace(value) || value == "-") return list;
    foreach (var part in value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var pair = part.Split('|', StringSplitOptions.TrimEntries);
        var times = pair[1].Split('-', StringSplitOptions.TrimEntries);
        list.Add(new UnavailableSlot
        {
            Date = DateOnly.Parse(pair[0]),
            StartTime = TimeOnly.Parse(times[0]),
            EndTime = TimeOnly.Parse(times[1])
        });
    }
    return list;
}

static List<ExamDutySlot> ParseSlots(string csv)
{
    var slots = new List<ExamDutySlot>();
    var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    foreach (var line in lines)
    {
        var p = line.Split(',', StringSplitOptions.TrimEntries);
        if (p.Length < 9 || !int.TryParse(p[0], out var id)) continue;
        slots.Add(new ExamDutySlot
        {
            Id = id,
            Date = DateOnly.Parse(p[1]),
            StartTime = TimeOnly.Parse(p[2]),
            EndTime = TimeOnly.Parse(p[3]),
            ShiftType = Enum.Parse<ShiftType>(p[4], ignoreCase: true),
            Grade = p[5],
            Subject = p[6],
            Venue = p[7],
            TeachersRequired = int.Parse(p[8])
        });
    }
    return slots;
}
