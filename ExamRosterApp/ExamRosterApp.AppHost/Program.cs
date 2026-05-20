var builder = DistributedApplication.CreateBuilder(args);

var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("examroster-sql", sqlPassword).WithLifetime(ContainerLifetime.Persistent);
var db = sql.AddDatabase("ExamRoster_LocalDev");

builder.AddProject<Projects.ExamRosterApp_Desktop>("desktop")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
