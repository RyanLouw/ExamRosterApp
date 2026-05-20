using Aspire.Hosting.ApplicationModel;

var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("examroster-sql", sqlPassword)
    .WithLifetime(ContainerLifetime.Persistent);

var db = sql.AddDatabase("ExamRoster_LocalDev");

builder.AddExecutable(
        name: "desktop",
        command: "dotnet",
        workingDirectory: "../ExamRosterApp.Desktop",
        args: "run --project ExamRosterApp.Desktop.csproj")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
