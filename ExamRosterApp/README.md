# ExamRosterApp

Local development uses an **Aspire-managed SQL Server** database named `ExamRoster_LocalDev`.
This setup is isolated for development/testing and must never use a live production connection string.

## Run locally
1. Start `ExamRosterApp.AppHost`.
2. Aspire starts the local SQL Server container and wires the `examrosterdb` connection into the Desktop app.
3. Open the Desktop app and use the **Migrations** tab to apply EF migrations to local DB.

## EF Core migrations
- Create migration:
  `dotnet ef migrations add InitialCreate --project ExamRosterApp.Infrastructure --startup-project ExamRosterApp.Desktop`
- Apply migration:
  `dotnet ef database update --project ExamRosterApp.Infrastructure --startup-project ExamRosterApp.Desktop`

## Confirm local DB only
- Verify connection string name is `examrosterdb`.
- Verify database name is `ExamRoster_LocalDev`.
- Development-only connection string is in `ExamRosterApp.AppHost/appsettings.Development.json`.

## Reset/recreate local DB
1. Stop AppHost.
2. Remove Aspire SQL container/volume from Aspire dashboard or Docker.
3. Restart AppHost.
4. Re-apply migrations from CLI or the **Migrations** tab in the desktop app.

## Production placeholder policy
Production connection strings are not stored in source control.
Use **user secrets** or **environment variables** when production wiring is needed later.
