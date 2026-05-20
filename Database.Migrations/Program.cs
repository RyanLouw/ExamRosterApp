using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger("ExamRosterMigrations");
logger.LogInformation("Database.Migrations project is restored in the solution. Configure and run migrations here.");
