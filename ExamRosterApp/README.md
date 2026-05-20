# ExamRosterApp

Local development uses **Aspire-managed SQL Server** with database name `ExamRoster_LocalDev`. Never use live connection strings here.

## Run
1. Start AppHost project.
2. Aspire starts local SQL Server container and Desktop app.

## Migrations
- Create: `dotnet ef migrations add InitialCreate --project ExamRosterApp.Infrastructure --startup-project ExamRosterApp.Desktop`
- Apply: `dotnet ef database update --project ExamRosterApp.Infrastructure --startup-project ExamRosterApp.Desktop`

## Confirm local DB
Check connection string key `examrosterdb` points to `ExamRoster_LocalDev` in `appsettings.Development.json`.

## Reset DB
Remove sql data volume/container from Aspire dashboard, then rerun migrations.

## Production placeholder
Production connection string must come from environment variables or user secrets only.
