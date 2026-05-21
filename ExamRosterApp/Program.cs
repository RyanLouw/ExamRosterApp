using System.Text;
using System.Globalization;
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
    private readonly DataGridView _statsGrid = new();
    private readonly TextBox _outputBox = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, Dock = DockStyle.Fill };
    private List<string> _latestDiagnostics = [];

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
        var exportButton = new Button { Text = "Export to Excel (CSV)", AutoSize = true };
        var exportLogButton = new Button { Text = "Export Logs", AutoSize = true };

        addTeacherButton.Click += (_, _) => _teachersGrid.Rows.Add();
        addSlotButton.Click += (_, _) => _slotsGrid.Rows.Add();
        generateButton.Click += (_, _) => GenerateRoster();
        clearButton.Click += (_, _) => _outputBox.Clear();
        exportButton.Click += (_, _) => ExportRosterCsv();
        exportLogButton.Click += (_, _) => ExportLatestDiagnostics();

        buttonPanel.Controls.Add(addTeacherButton);
        buttonPanel.Controls.Add(addSlotButton);
        buttonPanel.Controls.Add(generateButton);
        buttonPanel.Controls.Add(clearButton);
        buttonPanel.Controls.Add(exportButton);
        buttonPanel.Controls.Add(exportLogButton);
        root.Controls.Add(buttonPanel, 0, 0);

        ConfigureTeacherGrid();
        ConfigureSlotGrid();

        var tabs = new TabControl { Dock = DockStyle.Fill };

        var teacherTab = new TabPage("Teachers");
        teacherTab.Controls.Add(_teachersGrid);
        tabs.TabPages.Add(teacherTab);

        var slotTab = new TabPage("Exam Slots");
        slotTab.Controls.Add(_slotsGrid);
        tabs.TabPages.Add(slotTab);

        ConfigureStatsGrid();
        var statsTab = new TabPage("Teacher Stats");
        statsTab.Controls.Add(_statsGrid);
        tabs.TabPages.Add(statsTab);

        root.SetRowSpan(tabs, 2);
        root.Controls.Add(tabs, 0, 1);

        var outputGroup = new GroupBox { Text = "Generated Roster", Dock = DockStyle.Fill };
        outputGroup.Controls.Add(_outputBox);
        root.Controls.Add(outputGroup, 0, 3);

        Controls.Add(root);
        LoadDataFromDatabaseOrShowStatus();
    }

    private void LoadDataFromDatabaseOrShowStatus()
    {
        var connectionString = ResolveConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            _outputBox.Text = "No database connection string found. Set EXAMROSTER_DB_CONNECTION or ConnectionStrings__ExamDb.";
            return;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();

            LoadTeachers(connection);
            LoadExamSlots(connection);

            _outputBox.Text = $"Loaded {_teachersGrid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow)} teachers and {_slotsGrid.Rows.Cast<DataGridViewRow>().Count(r => !r.IsNewRow)} exam slots from the database.";
        }
        catch (Exception ex)
        {
            _outputBox.Text = $"Database load failed: {ex.Message}";
        }
    }

    private static string? ResolveConnectionString()
    {
        return Environment.GetEnvironmentVariable("EXAMROSTER_DB_CONNECTION")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__ExamDb")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:ExamDb");
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
        var loadedRows = LoadExamDutySlots(connection);
        if (loadedRows > 0)
            return;

        LoadExamSlotsFromExamPaper(connection);
    }

    private int LoadExamDutySlots(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT ExamDutySlotId, [Date], StartTime, EndTime, ShiftTypeId,
                       Grade, Subject, Venue, TeachersRequired, LearnerCount, LearnersPerInvigilator
                FROM tr.ExamDutySlot
                WHERE IsActive = 1
                ORDER BY [Date], StartTime, Venue;";

        using var reader = command.ExecuteReader();
        var loaded = 0;

        while (reader.Read())
        {
            var shiftId = Convert.ToInt32(reader["ShiftTypeId"]);
            var shiftName = Enum.IsDefined(typeof(ShiftType), shiftId)
                ? ((ShiftType)shiftId).ToString()
                : ShiftType.Morning.ToString();

            _slotsGrid.Rows.Add(
                Convert.ToInt32(reader["ExamDutySlotId"]),
                ReadDateOnly(reader["Date"]).ToString("yyyy-MM-dd"),
                ReadTimeOnly(reader["StartTime"]).ToString("HH:mm"),
                ReadTimeOnly(reader["EndTime"]).ToString("HH:mm"),
                shiftName,
                reader["Grade"].ToString() ?? string.Empty,
                reader["Subject"].ToString() ?? string.Empty,
                reader["Venue"].ToString() ?? string.Empty,
                Convert.ToInt32(reader["TeachersRequired"]),
                reader["LearnerCount"] is DBNull ? string.Empty : reader["LearnerCount"],
                reader["LearnersPerInvigilator"] is DBNull ? string.Empty : reader["LearnersPerInvigilator"]);

            loaded++;
        }

        return loaded;
    }

    private void LoadExamSlotsFromExamPaper(SqlConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
                SELECT ExamPaperId, [Date], StartTime, EndTime, ShiftTypeId,
                       Grade, Subject
                FROM tr.ExamPaper
                WHERE IsActive = 1 AND IsSchoolHoliday = 0
                ORDER BY [Date], StartTime, Grade;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var shiftId = Convert.ToInt32(reader["ShiftTypeId"]);
            var shiftName = Enum.IsDefined(typeof(ShiftType), shiftId)
                ? ((ShiftType)shiftId).ToString()
                : ShiftType.Morning.ToString();

            _slotsGrid.Rows.Add(
                Convert.ToInt32(reader["ExamPaperId"]),
                ReadDateOnly(reader["Date"]).ToString("yyyy-MM-dd"),
                ReadTimeOnly(reader["StartTime"]).ToString("HH:mm"),
                ReadTimeOnly(reader["EndTime"]).ToString("HH:mm"),
                shiftName,
                reader["Grade"].ToString() ?? string.Empty,
                reader["Subject"].ToString() ?? string.Empty,
                "TBD",
                1,
                string.Empty,
                string.Empty);
        }
    }


    private static DateOnly ReadDateOnly(object value)
    {
        if (value is DateOnly d) return d;
        if (value is DateTime dt) return DateOnly.FromDateTime(dt);
        return DateOnly.Parse(value.ToString() ?? throw new InvalidOperationException("Date value is null."));
    }

    private static TimeOnly ReadTimeOnly(object value)
    {
        if (value is TimeOnly t) return t;
        if (value is TimeSpan ts) return TimeOnly.FromTimeSpan(ts);
        if (value is DateTime dt) return TimeOnly.FromDateTime(dt);
        return TimeOnly.Parse(value.ToString() ?? throw new InvalidOperationException("Time value is null."));
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
        _slotsGrid.Columns.Add("IntervalCount", "Interval Count (optional)");
        _slotsGrid.Columns.Add("TeachersPerInterval", "Teachers per Interval (optional)");
    }

    private void GenerateRoster()
    {
        try
        {
            var teachers = ReadTeachers();
            var slots = ReadSlots();
            ShowJuniorGradeHintIfNeeded(slots);

            var generator = new RosterGenerator();
            var result = generator.GenerateRoster(teachers, slots);
            _latestDiagnostics = result.Diagnostics.ToList();

            var slotById = slots.ToDictionary(s => s.Id);
            var teacherById = teachers.ToDictionary(t => t.Id);

            var sb = new StringBuilder();
            sb.AppendLine("Date       Time        Duration  Shift      Subject                 Venue      Teacher     Interval Plan");
            sb.AppendLine("--------------------------------------------------------------------------------------------------------------");

            foreach (var assignment in result.Assignments
                         .OrderBy(a => slotById[a.ExamDutySlotId].Date)
                         .ThenBy(a => slotById[a.ExamDutySlotId].StartTime)
                         .ThenBy(a => slotById[a.ExamDutySlotId].Venue))
            {
                var slot = slotById[assignment.ExamDutySlotId];
                var teacher = teacherById[assignment.TeacherId];
                sb.AppendLine($"{slot.Date:yyyy-MM-dd} {slot.StartTime:HH:mm}-{slot.EndTime:HH:mm} {slot.DurationMinutes,4}m    {slot.ShiftType,-10} {slot.Subject,-22} {slot.Venue,-10} {teacher.FullName,-12} {BuildIntervalPlan(slot)}");
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
            PopulateTeacherStatsGrid(teachers, slots, result.Assignments);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Please fix input data: {ex.Message}", "Invalid input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ShowJuniorGradeHintIfNeeded(List<ExamDutySlot> slots)
    {
        if (slots.Any(s => s.Grade.Contains("Grade 8", StringComparison.OrdinalIgnoreCase)
            || s.Grade.Contains("Grade 9", StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(
                "Reminder: Grade 8 and Grade 9 usually require 1 teacher per test.",
                "Grade 8/9 Hint",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }

    private void ExportRosterCsv()
    {
        try
        {
            var teachers = ReadTeachers();
            var slots = ReadSlots();
            var generator = new RosterGenerator();
            var result = generator.GenerateRoster(teachers, slots);

            var slotById = slots.ToDictionary(s => s.Id);
            var teacherById = teachers.ToDictionary(t => t.Id);

            using var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = $"exam-roster-{DateTime.Now:yyyyMMdd-HHmm}.csv"
            };

            if (saveDialog.ShowDialog() != DialogResult.OK)
                return;

            var delimiter = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            var csv = new StringBuilder();
            csv.AppendLine(string.Join(delimiter, new[]
            {
                Csv("Teacher", delimiter), Csv("Date", delimiter), Csv("Start", delimiter), Csv("End", delimiter), Csv("DurationMinutes", delimiter),
                Csv("Shift", delimiter), Csv("Grade", delimiter), Csv("Subject", delimiter), Csv("Venue", delimiter), Csv("IntervalPlan", delimiter)
            }));

            foreach (var assignment in result.Assignments
                         .OrderBy(a => teacherById[a.TeacherId].FullName)
                         .ThenBy(a => slotById[a.ExamDutySlotId].Date)
                         .ThenBy(a => slotById[a.ExamDutySlotId].StartTime))
            {
                var teacher = teacherById[assignment.TeacherId];
                var slot = slotById[assignment.ExamDutySlotId];
                csv.AppendLine(string.Join(delimiter, new[]
                {
                    Csv(teacher.FullName, delimiter), Csv(slot.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), delimiter),
                    Csv(slot.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture), delimiter), Csv(slot.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture), delimiter), Csv(slot.DurationMinutes.ToString(CultureInfo.InvariantCulture), delimiter),
                    Csv(slot.ShiftType.ToString(), delimiter), Csv(slot.Grade, delimiter), Csv(slot.Subject, delimiter), Csv(slot.Venue, delimiter), Csv(BuildIntervalPlan(slot), delimiter)
                }));
            }

            File.WriteAllText(saveDialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show($"Exported roster to {saveDialog.FileName}", "Export complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Export error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportLatestDiagnostics()
    {
        if (_latestDiagnostics.Count == 0)
        {
            MessageBox.Show("No diagnostics yet. Generate a roster first.", "No logs", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var saveDialog = new SaveFileDialog
        {
            Filter = "Text files (*.txt)|*.txt",
            FileName = $"roster-diagnostics-{DateTime.Now:yyyyMMdd-HHmm}.txt"
        };

        if (saveDialog.ShowDialog() != DialogResult.OK)
            return;

        File.WriteAllLines(saveDialog.FileName, _latestDiagnostics, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        MessageBox.Show($"Saved diagnostics to {saveDialog.FileName}", "Logs exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static string Csv(string value, string delimiter)
    {
        var escaped = value.Replace("\"", "\"\"");
        var needsQuotes = escaped.Contains(delimiter, StringComparison.Ordinal)
            || escaped.Contains("\"", StringComparison.Ordinal)
            || escaped.Contains("\n", StringComparison.Ordinal)
            || escaped.Contains("\r", StringComparison.Ordinal);

        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private void ConfigureStatsGrid()
    {
        _statsGrid.Dock = DockStyle.Fill;
        _statsGrid.ReadOnly = true;
        _statsGrid.AllowUserToAddRows = false;
        _statsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _statsGrid.Columns.Add("Teacher", "Teacher");
        _statsGrid.Columns.Add("TotalDuties", "Total Duties");
        _statsGrid.Columns.Add("TotalHours", "Total Hours");
        _statsGrid.Columns.Add("MorningHours", "Morning Hours");
        _statsGrid.Columns.Add("AfternoonHours", "Afternoon Hours");
    }

    private void PopulateTeacherStatsGrid(List<Teacher> teachers, List<ExamDutySlot> slots, List<DutyAssignment> assignments)
    {
        _statsGrid.Rows.Clear();
        var teacherById = teachers.ToDictionary(t => t.Id);
        var slotById = slots.ToDictionary(s => s.Id);

        foreach (var group in assignments.GroupBy(a => a.TeacherId).OrderBy(g => teacherById[g.Key].FullName))
        {
            var assignedSlots = group.Select(a => slotById[a.ExamDutySlotId]).ToList();
            var totalMinutes = assignedSlots.Sum(s => s.DurationMinutes);
            var morningMinutes = assignedSlots.Where(s => s.ShiftType == ShiftType.Morning).Sum(s => s.DurationMinutes);
            var afternoonMinutes = assignedSlots.Where(s => s.ShiftType == ShiftType.Afternoon).Sum(s => s.DurationMinutes);

            _statsGrid.Rows.Add(
                teacherById[group.Key].FullName,
                assignedSlots.Count,
                FormatHours(totalMinutes),
                FormatHours(morningMinutes),
                FormatHours(afternoonMinutes));
        }
    }

    private static string FormatHours(int minutes)
        => $"{minutes / 60}h {minutes % 60}m";

    private static string BuildIntervalPlan(ExamDutySlot slot)
    {
        var intervalCount = slot.LearnerCount.GetValueOrDefault();
        var teachersPerInterval = slot.LearnersPerInvigilator.GetValueOrDefault();

        if (intervalCount <= 0)
            return "Single interval (full test)";

        var intervalMinutes = Math.Max(1, slot.DurationMinutes / intervalCount);
        var teacherText = teachersPerInterval > 0 ? $", {teachersPerInterval} teacher(s)/interval" : string.Empty;
        return $"{intervalCount} interval(s) x {intervalMinutes} min{teacherText}";
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
                LearnerCount = ParseNullableInt(row, "IntervalCount"),
                LearnersPerInvigilator = ParseNullableInt(row, "TeachersPerInterval")
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
