var builder = DistributedApplication.CreateBuilder(args);
var sql = builder.AddSqlServer("examroster-sql").WithDataVolume();
var db = sql.AddDatabase("examrosterdb", "ExamRoster_LocalDev");
builder.AddProject<Projects.ExamRosterApp_Desktop>("desktop").WithReference(db);
builder.Build().Run();
