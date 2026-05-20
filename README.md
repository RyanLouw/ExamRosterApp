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


## Open in IDE
- Open `ExamRosterApp.sln` from the repository root.
- You should see all projects, including `ExamRosterApp.AppHost` (Aspire host) in Solution Explorer.


## Compatibility note
- `ExamRosterApp.AppHost` targets `net8.0-windows` so it can reference and orchestrate the WPF desktop project (`net8.0-windows`) without TFM mismatch errors.
- The solution uses NuGet-based Aspire hosting (`Aspire.Hosting.AppHost` package) and does not require the deprecated Aspire workload SDK.


## Aspire SDK note
- This repo uses `Microsoft.NET.Sdk` + NuGet package `Aspire.Hosting.AppHost` only.
- It does **not** use the deprecated Aspire Workload SDK.
- If your local tooling still shows a workload deprecation warning, update Visual Studio/.NET SDK to the latest .NET 8 servicing release and restore packages again (`dotnet restore`).


## AppHost compile notes
- `ExamRosterApp.AppHost` uses `Aspire.Hosting.SqlServer` for `AddSqlServer(...)` and `ContainerLifetime`.
- The desktop app is launched via `AddExecutable(...)` (dotnet run) instead of generated `Projects.*` types, avoiding missing `Projects.ExamRosterApp_Desktop` compile errors.
