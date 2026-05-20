# ExamRosterApp

Local development is isolated using .NET Aspire + local SQL Server container database `ExamRoster_LocalDev`.

## Run local dev
1. Start AppHost: `dotnet run --project ExamRosterApp.AppHost`
2. AppHost starts local SQL Server container and wires connection to desktop app.

## EF Migrations
- Create migration:
`dotnet ef migrations add InitialCreate -p ExamRosterApp.Infrastructure -s ExamRosterApp.Desktop -o Data/Migrations`
- Apply migration:
`dotnet ef database update -p ExamRosterApp.Infrastructure -s ExamRosterApp.Desktop`

## Safety
- Never place production connection strings in source code.
- Production connection should come from User Secrets or environment variables only.

## Verify using local DB
- In Aspire dashboard, ensure resource name is `ExamRosterDb` and DB name is `ExamRoster_LocalDev`.

## Reset local DB
- Stop AppHost, remove SQL container/volume, restart AppHost, rerun `dotnet ef database update`.
