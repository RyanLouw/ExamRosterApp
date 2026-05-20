using Database.Migrations;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ExamRosterApp.DbMigration;

public static class Program
{
    private sealed record MigratorTag(string ConnectionKey, string Tag);

    private static readonly MigratorTag[] MigratorTags =
    [
        new("ExamDb", TagNames.ToetsRooster)
    ];

    public static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            Log.Information("Starting ExamRoster DB migration runner");

            var configSettings = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            foreach (var tag in MigratorTags)
            {
                Log.Information(
                    "Running migrations for ConnectionKey: {ConnectionKey}, Tag: {Tag}",
                    tag.ConnectionKey,
                    tag.Tag);

                using var serviceProvider = CreateServices(tag, configSettings);
                using var scope = serviceProvider.CreateScope();

                MigrateUp(scope.ServiceProvider);

                Log.Information("Finished migrations for {ConnectionKey}", tag.ConnectionKey);
            }

            Log.Information("All ExamRoster migrations completed successfully");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Migration runner failed");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static ServiceProvider CreateServices(
        MigratorTag migratorTag,
        IConfigurationRoot config)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Configuration.AddConfiguration(config);

        var connection = builder.Configuration.GetConnectionString(migratorTag.ConnectionKey);

        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException(
                $"Connection string '{migratorTag.ConnectionKey}' was not found. " +
                "In Aspire, this must match the database resource name, e.g. 'ExamDb'.");
        }

        builder.Services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                rb.AddSqlServer()
                  .WithGlobalConnectionString(connection)
                  .ScanIn(typeof(Program).Assembly)
                  .For.Migrations();
            })
            .AddLogging(lb =>
            {
                lb.AddFluentMigratorConsole();
            })
            .Configure<RunnerOptions>(opt =>
            {
                opt.Tags = [migratorTag.Tag];
            });

        return builder.Services.BuildServiceProvider(validateScopes: true);
    }

    private static void MigrateUp(IServiceProvider services)
    {
        var runner = services.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
    }
}