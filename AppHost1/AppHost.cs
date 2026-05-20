var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("examroster-sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("examroster-sql-data");

var examDb = sql.AddDatabase("ExamDb");

var migrations = builder.AddProject<Projects.Database_Migrations>("database-migrations")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(examDb)
    .WaitFor(examDb);

builder.AddProject<Projects.ExamRosterApp>("desktop")
    .WithExplicitStart()
    .WithReference(examDb)
    .WaitFor(examDb)
    .WaitForCompletion(migrations);

await builder.Build().RunAsync();