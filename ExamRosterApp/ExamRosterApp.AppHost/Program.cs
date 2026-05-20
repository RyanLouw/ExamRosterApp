var builder = DistributedApplication.CreateBuilder(args);
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);
var db = sql.AddDatabase("ExamRosterDb", "ExamRoster_LocalDev");
builder.AddProject("desktop", "../ExamRosterApp.Desktop/ExamRosterApp.Desktop.csproj")
    .WithReference(db);
builder.Build().Run();
