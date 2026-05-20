# ExamRosterApp

Local-first school exam duty roster application (WPF + .NET 8 + EF Core + SQL Server + Aspire).

## Safety
- Development uses **Aspire-managed local SQL Server** database named `ExamRoster_LocalDev`.
- Do not place live/prod connection strings in source code.
- For future production deployment, use environment variables or user secrets only.

## Run locally
1. Start AppHost: `dotnet run --project ExamRosterApp/ExamRosterApp.AppHost`
2. Apply migrations (after installing .NET SDK):
   - `dotnet ef database update --project ExamRosterApp/ExamRosterApp.Infrastructure --startup-project ExamRosterApp/ExamRosterApp.Desktop`
3. Launch desktop app from Aspire dashboard or project.

## Verify local DB usage
- In Aspire dashboard, confirm SQL Server container `examroster-sql` is running.
- Confirm database name is `ExamRoster_LocalDev`.

## Reset local DB
- Stop AppHost and remove/recreate local SQL container volume.
- Re-run migrations.
