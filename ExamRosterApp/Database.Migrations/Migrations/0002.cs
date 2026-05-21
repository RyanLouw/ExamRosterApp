using FluentMigrator;

namespace Database.Migrations;

[Tags(TagNames.ToetsRooster)]
[Migration(0002)]
public class _0002_ExamPaper : Migration
{
    public override void Up()
    {
        Execute.Script(@"Migrations\Scripts\0002_ExamPaper.sql");
    }

    public override void Down()
    {
    }
}