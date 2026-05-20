var builder = DistributedApplication.CreateBuilder(args);


var postgres = builder.AddPostgres("rentalapp-postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("rentalapp-postgres");

var ExameDb = postgres.AddDatabase("ExameDb");


var migrations = builder.AddProject<>("database-migrations")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithReference(ExameDb)
    .WaitFor(ExameDb);


builder.AddProject<Projects.ExamRosterApp>("web")
    .WithExplicitStart()
    .WithReference(ExameDb);
    

await builder.Build().RunAsync();