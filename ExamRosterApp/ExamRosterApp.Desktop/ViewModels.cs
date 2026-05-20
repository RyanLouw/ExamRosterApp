using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ExamRosterApp.Application;
using ExamRosterApp.Domain;

namespace ExamRosterApp.Desktop;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IRosterService _roster;
    private readonly ITeacherService _teacher;
    private readonly IExamDutySlotService _slot;
    private readonly IDatabaseSetupService _db;

    public ObservableCollection<Teacher> Teachers { get; } = new();
    public ObservableCollection<ExamDutySlot> Slots { get; } = new();
    public ObservableCollection<object> Assignments { get; } = new();
    public ObservableCollection<TeacherDutySummary> Fairness { get; } = new();

    private string _warningsText = string.Empty;
    public string WarningsText { get => _warningsText; set { _warningsText = value; OnPropertyChanged(); } }

    private string _migrationStatus = "Not started.";
    public string MigrationStatus { get => _migrationStatus; set { _migrationStatus = value; OnPropertyChanged(); } }

    public ICommand GenerateRosterCommand { get; }
    public ICommand ClearRosterCommand { get; }
    public ICommand ApplyMigrationsCommand { get; }

    public MainViewModel(IRosterService roster, ITeacherService teacher, IExamDutySlotService slot, IDatabaseSetupService db)
    {
        _roster = roster; _teacher = teacher; _slot = slot; _db = db;
        GenerateRosterCommand = new AsyncRelay(async () => { var r = await _roster.GenerateAndSaveRosterAsync(); WarningsText = string.Join(Environment.NewLine, r.Warnings); await LoadAsync(); });
        ClearRosterCommand = new AsyncRelay(async () => { await _roster.ClearRosterAsync(); await LoadAsync(); });
        ApplyMigrationsCommand = new AsyncRelay(async () => { await _db.ApplyMigrationsAsync(); MigrationStatus = "Migrations applied successfully to local dev DB."; await LoadAsync(); });
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        Teachers.Clear(); foreach (var t in await _teacher.GetTeachersAsync()) Teachers.Add(t);
        Slots.Clear(); foreach (var s in await _slot.GetExamDutySlotsAsync()) Slots.Add(s);
        Assignments.Clear(); foreach (var a in await _roster.GetCurrentRosterAsync()) Assignments.Add(new { a.Slot.Date, Time = $"{a.Slot.StartTime}-{a.Slot.EndTime}", a.Slot.ShiftType, a.Slot.Grade, a.Slot.Subject, a.Slot.Venue, Teacher = a.Teacher.FullName });
        Fairness.Clear(); foreach (var f in await _roster.GetTeacherDutySummaryAsync()) Fairness.Add(f);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class AsyncRelay(Func<Task> run) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? p) => true;
    public async void Execute(object? p) => await run();
}
