using System.Text;
using System.Data.SqlClient;
using System.Windows.Forms;
using ExamRosterApp.Rostering;

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
    private readonly DataGridView _teachersGrid = new();
    private readonly DataGridView _slotsGrid = new();
    private readonly TextBox _outputBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill };

    public RosterForm()
    {
        Text = "Exam Duty Scheduler";
        Width = 1200;
        Height = 800;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

        var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
        var addTeacherButton = new Button { Text = "Add Teacher", AutoSize = true };
        var addSlotButton = new Button { Text = "Add Exam Slot", AutoSize = true };
        var generateButton = new Button { Text = "Generate Roster", AutoSize = true };
        var clearButton = new Button { Text = "Clear Output", AutoSize = true };

        addTeacherButton.Click += (_, _) => _teachersGrid.Rows.Add();
        addSlotButton.Click += (_, _) => _slotsGrid.Rows.Add();
        generateButton.Click += (_, _) => GenerateRoster();
        clearButton.Click += (_, _) => _outputBox.Clear();

        buttonPanel.Controls.Add(addTeacherButton);
        buttonPanel.Controls.Add(addSlotButton);
        buttonPanel.Controls.Add(generateButton);
        buttonPanel.Controls.Add(clearButton);
        root.Controls.Add(buttonPanel, 0, 0);

        ConfigureTeacherGrid();
        ConfigureSlotGrid();

        var teacherGroup = new GroupBox { Text = "Teachers", Dock = DockStyle.Fill };
        teacherGroup.Controls.Add(_teachersGrid);
        root.Controls.Add(teacherGroup, 0, 1);

        var slotGroup = new GroupBox { Text = "Exam Slots", Dock = DockStyle.Fill };
        slotGroup.Controls.Add(_slotsGrid);
        root.Controls.Add(slotGroup, 0, 2);

        var outputGroup = new GroupBox { Text = "Generated Roster", Dock = DockStyle.Fill };
        outputGroup.Controls.Add(_outputBox);
        root.Controls.Add(outputGroup, 0, 3);

        Controls.Add(root);
        LoadDataFromDatabase();
    }

    private void LoadDataFromDatabase()
    {
        var connectionString = Environment.GetEnvironmentVariable("EXAMROSTER_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            LoadTeachers(connection);
            LoadExamSlots(connection);
        }
        catch
        {
            // Leave grids empty if database cannot be reached.
        }
    }

    private void LoadTeachers(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT TeacherId, FullName, CanWorkMorning, CanWorkAfternoon, IsActive,
                       ISNULL(TeacherGroupId, 1) AS TeacherGroupId,
                       MinDutyMinutes, MaxDutyMinutes
                FROM tr.Teacher
                WHERE IsActive = 1
                ORDER BY FullName;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var groupId = Convert.ToInt32(reader["TeacherGroupId"]);
            var groupName = Enum.IsDefined(typeof(TeacherGroup), groupId)
                ? ((TeacherGroup)groupId).ToString()
                : TeacherGroup.Open.ToString();

            _teachersGrid.Rows.Add(
                Convert.ToInt32(reader["TeacherId"]),
                reader["FullName"].ToString() ?? string.Empty,
                groupName,
                Convert.ToBoolean(reader["CanWorkMorning"]),
                Convert.ToBoolean(reader["CanWorkAfternoon"]),
                string.Empty,
                reader["MinDutyMinutes"] is DBNull ? string.Empty : reader["MinDutyMinutes"],
                reader["MaxDutyMinutes"] is DBNull ? string.Empty : reader["MaxDutyMinutes"],
                "-");
        }
    }

    private void LoadExamSlots(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT ExamDutySlotId, DutyDate, StartTime, EndTime, ShiftType,
                       Grade, Subject, Venue, TeachersRequired, LearnerCount, LearnersPerInvigilator
                FROM tr.ExamDutySlot
                ORDER BY DutyDate, StartTime, Venue;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var shiftValue = reader["ShiftType"].ToString() ?? ShiftType.Morning.ToString();
            var shiftName = Enum.TryParse<ShiftType>(shiftValue, true, out var shift)
                ? shift.ToString()
                : ShiftType.Morning.ToString();

            _slotsGrid.Rows.Add(
                Convert.ToInt32(reader["ExamDutySlotId"]),
                DateOnly.FromDateTime(Convert.ToDateTime(reader["DutyDate"])).ToString("yyyy-MM-dd"),
                TimeOnly.FromDateTime(Convert.ToDateTime(reader["StartTime"])).ToString("HH:mm"),
                TimeOnly.FromDateTime(Convert.ToDateTime(reader["EndTime"])).ToString("HH:mm"),
                shiftName,
                reader["Grade"].ToString() ?? string.Empty,
                reader["Subject"].ToString() ?? string.Empty,
                reader["Venue"].ToString() ?? string.Empty,
                Convert.ToInt32(reader["TeachersRequired"]),
                reader["LearnerCount"] is DBNull ? string.Empty : reader["LearnerCount"],
                reader["LearnersPerInvigilator"] is DBNull ? string.Empty : reader["LearnersPerInvigilator"]);
        }
    }

    private void ConfigureTeacherGrid()
    {
        _teachersGrid.Dock = DockStyle.Fill;
        _teachersGrid.AllowUserToAddRows = true;
        _teachersGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _teachersGrid.Columns.Add("Id", "Id");
        _teachersGrid.Columns.Add("FullName", "Teacher Name");
        var teacherGroupColumn = new DataGridViewComboBoxColumn
        {
            Name = "Group",
            HeaderText = "Group",
            DataSource = Enum.GetNames(typeof(TeacherGroup))
        };
        _teachersGrid.Columns.Add(teacherGroupColumn);
        _teachersGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanWorkMorning", HeaderText = "Can Work Morning" });
        _teachersGrid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "CanWorkAfternoon", HeaderText = "Can Work Afternoon" });
        _teachersGrid.Columns.Add("HomeSubject", "Home Subject");
        _teachersGrid.Columns.Add("MinDutyMinutes", "Min Duty Minutes (optional)");
        _teachersGrid.Columns.Add("MaxDutyMinutes", "Max Duty Minutes (optional)");
        _teachersGrid.Columns.Add("Unavailable", "Unavailable (yyyy-MM-dd|HH:mm-HH:mm;...) ");
    }

    private void ConfigureSlotGrid()
    {
        _slotsGrid.Dock = DockStyle.Fill;
        _slotsGrid.AllowUserToAddRows = true;
        _slotsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _slotsGrid.Columns.Add("Id", "Id");
        _slotsGrid.Columns.Add("Date", "Date (yyyy-MM-dd)");
        _slotsGrid.Columns.Add("StartTime", "Start (HH:mm)");
        _slotsGrid.Columns.Add("EndTime", "End (HH:mm)");

        var shiftColumn = new DataGridViewComboBoxColumn
        {
            Name = "ShiftType",
            HeaderText = "Shift",
            DataSource = Enum.GetNames(typeof(ShiftType))
        };
        _slotsGrid.Columns.Add(shiftColumn);

        _slotsGrid.Columns.Add("Grade", "Grade");
        _slotsGrid.Columns.Add("Subject", "Subject");
        _slotsGrid.Columns.Add("Venue", "Venue");
        _slotsGrid.Columns.Add("TeachersRequired", "Teachers Required");
        _slotsGrid.Columns.Add("LearnerCount", "Learner Count (optional)");
        _slotsGrid.Columns.Add("LearnersPerInvigilator", "Learners per Invigilator (optional)");
    }

    private void GenerateRoster()
    {
        try
        {
            var teachers = ReadTeachers();
            var slots = ReadSlots();

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
            MessageBox.Show($"Please fix input data: {ex.Message}", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private List<Teacher> ReadTeachers()
    {
        var list = new List<Teacher>();
        foreach (DataGridViewRow row in _teachersGrid.Rows)
        {
            if (row.IsNewRow || row.Cells["Id"].Value is null) continue;
            list.Add(new Teacher
            {
                Id = ParseInt(row, "Id"),
                FullName = ReadString(row, "FullName"),
                Group = Enum.Parse<TeacherGroup>(ReadString(row, "Group"), true),
                CanWorkMorning = ParseBool(row, "CanWorkMorning"),
                CanWorkAfternoon = ParseBool(row, "CanWorkAfternoon"),
                HomeSubject = EmptyToNull(ReadString(row, "HomeSubject")),
                MinDutyMinutes = ParseNullableInt(row, "MinDutyMinutes"),
                MaxDutyMinutes = ParseNullableInt(row, "MaxDutyMinutes"),
                UnavailableSlots = ParseUnavailable(ReadString(row, "Unavailable"))
            });
        }
        return list;
    }

    private List<ExamDutySlot> ReadSlots()
    {
        var list = new List<ExamDutySlot>();
        foreach (DataGridViewRow row in _slotsGrid.Rows)
        {
            if (row.IsNewRow || row.Cells["Id"].Value is null) continue;
            list.Add(new ExamDutySlot
            {
                Id = ParseInt(row, "Id"),
                Date = DateOnly.Parse(ReadString(row, "Date")),
                StartTime = TimeOnly.Parse(ReadString(row, "StartTime")),
                EndTime = TimeOnly.Parse(ReadString(row, "EndTime")),
                ShiftType = Enum.Parse<ShiftType>(ReadString(row, "ShiftType"), true),
                Grade = ReadString(row, "Grade"),
                Subject = ReadString(row, "Subject"),
                Venue = ReadString(row, "Venue"),
                TeachersRequired = ParseInt(row, "TeachersRequired"),
                LearnerCount = ParseNullableInt(row, "LearnerCount"),
                LearnersPerInvigilator = ParseNullableInt(row, "LearnersPerInvigilator")
            });
        }
        return list;
    }

    private static string ReadString(DataGridViewRow row, string column)
        => row.Cells[column].Value?.ToString()?.Trim() ?? string.Empty;

    private static int ParseInt(DataGridViewRow row, string column)
        => int.Parse(ReadString(row, column));

    private static bool ParseBool(DataGridViewRow row, string column)
        => row.Cells[column].Value is bool b ? b : bool.Parse(ReadString(row, column));

    private static int? ParseNullableInt(DataGridViewRow row, string column)
    {
        var value = ReadString(row, column);
        return string.IsNullOrWhiteSpace(value) ? null : int.Parse(value);
    }

    private static string? EmptyToNull(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static List<UnavailableSlot> ParseUnavailable(string value)
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
}
