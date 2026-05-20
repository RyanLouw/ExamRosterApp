using ExamRosterApp.Application;
using ExamRosterApp.Domain;
using Xunit;

public class RosterGeneratorTests
{
    [Fact]
    public async Task ReturnsWarningWhenInsufficientTeachers()
    {
        var gen = new RosterGenerator();
        var teachers = new List<Teacher>();
        var slots = new List<ExamDutySlot>{ new(){ Id=1, Date=DateOnly.FromDateTime(DateTime.Today), StartTime=new TimeOnly(9,0), EndTime=new TimeOnly(11,0), ShiftType=ShiftType.Morning, Subject="Math", Grade="10", Venue="A", TeachersRequired=1 } };
        var result = await gen.GenerateRosterAsync(teachers, slots, []);
        Assert.NotEmpty(result.Warnings);
    }
}
