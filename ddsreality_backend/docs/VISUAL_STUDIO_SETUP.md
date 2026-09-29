# Running in Visual Studio

## Verified findings (2026-09-29)

- Solution build succeeded with zero warnings and zero errors.
- Corrected the saved Visual Studio debug profile from `https` to `DdsCms`.
- A valid Windows HTTPS development certificate exists; the app starts outside the execution sandbox.
- HTTPS `/account/login` returned 200. Requests used certificate-validation bypass for this diagnostic, so browser trust was not verified.
- HTTPS `/` returned 500: SQL Server reported `Invalid object name 'dds.Sites'` (error 208). The configured database is reachable, but the required table is unavailable. Check the target database and install the CMS schema if it has not been provisioned.
- No database schema or content changes were performed during these checks.

1. Open `ddsreality_backend.sln` and set `ddsreality_backend` as the startup project.
2. Select the `DdsCms` launch profile beside the Start button.
3. Build the solution and press F5. The HTTPS address is `https://localhost:7240` and the admin login is `/account/login`.

The project targets .NET 8 and references `Microsoft.EntityFrameworkCore.SqlServer` 8.0.24. The solution built successfully on the inspected machine with its existing SDK and restored packages; no additional NuGet package was needed for that build. Visual Studio should have the ASP.NET and web development workload enabled.

## HTTPS startup error

If startup reports that the default developer certificate could not be found or is out of date, run the following in a terminal under the same Windows account that runs Visual Studio:

```powershell
dotnet dev-certs https --trust
```

Accept the Windows certificate trust prompt, restart the application, and use the HTTPS URL. Admin authentication uses secure cookies and requires HTTPS.

## Database errors

The application uses SQL Server through `ConnectionStrings:DefaultConnection`. A successful build or login-page load does not verify database access. Connection failures require checking the configured SQL Server, credentials, database permissions, and network access.

If the error mentions an invalid object such as `dds.Sites`, check whether the CMS schema has been installed. `Database/01_create_dds_ecommerce.sql` is a first-install script: review it and back up the database before running it in SQL Server Management Studio. Do not drop existing tables to rerun the script.

If the schema exists but site settings are missing, the provided seed command inserts missing initial records:

```powershell
dotnet run --project ddsreality_backend.csproj -- --seed
```

This command writes to the configured database. Use it only for the database you intend to initialize. Administrator creation is a separate offline command in `Program.cs` using `Bootstrap:Email` and `Bootstrap:Password` configuration.

Use the solution or explicit project filename for CLI commands because this directory also contains `DdsCms.csproj`.
