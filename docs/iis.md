# Windows IIS deployment

1. Install the ASP.NET Core .NET 8 Hosting Bundle on Windows Server 2019/2022 x64 and restart IIS.
2. Create an application pool: No Managed Code, Integrated pipeline, 64 bit. Use a dedicated identity with write access only to logs/data-protection storage where required.
3. Build locally: `powershell -File scripts/Publish-IIS.ps1`. Copy `artifacts/publish` to the IIS site directory during a maintenance window (`app_offline.htm` while replacing binaries).
4. Bind the actual hostname with a valid HTTPS certificate. Set `AllowedHosts` to the hostname. Production cookies require HTTPS.
5. Configure the app pool/site environment or an approved secret store with `ConnectionStrings__DefaultConnection`, SMTP settings and payment instructions. Development User Secrets do not get published. Keep credentials out of source and build artifacts.
6. Apply `sql/migrate.sql` using a migration identity, or run `dotnet AmHerb.Web.dll --migrate` from the publish folder with the connection environment configured. SQL scripts are idempotent. Back up an existing database before a later upgrade.
7. Bootstrap a NEW SuperAdmin email once using `AMHERB_ADMIN_EMAIL` and `AMHERB_ADMIN_PASSWORD` and the `--migrate` command; remove bootstrap variables afterward. No known default admin exists and an existing public registration is never silently elevated.
8. Run the site with `ASPNETCORE_ENVIRONMENT=Production`. Assign a dedicated SQL login with application CRUD permissions; schema migrations require a separate more privileged identity. Preserve the append-only triggers.
9. Persist ASP.NET Core Data Protection keys for the app-pool identity (load user profile or configure an ACL-protected persistent shared key location for multiple instances). Protect keys at rest using Windows facilities. Losing these keys invalidates authentication/guest access cookies.
10. Set application pool Start Mode=AlwaysRunning and site preload enabled. Keep the app alive if background jobs are expected to run continuously; recycle-safe jobs use database event keys and may retry after a deadlock. For larger deployments move jobs to a dedicated worker/scheduler.
11. Validate login/MFA, SMTP confirmation and reset, receive a real stock lot, perform a controlled sale/return, and verify shifts before opening to users.

## Environment keys

| Key | Purpose |
|---|---|
| ConnectionStrings__DefaultConnection | SQL Server application database |
| AMHERB_ADMIN_EMAIL / AMHERB_ADMIN_PASSWORD | First-run new SuperAdmin only |
| Smtp__Host / Port / EnableSsl / From / Username / Password | Confirmation/reset email |
| Payments__BankInstructions | Bank details shown on unpaid orders |
| Authentication__Google__ClientId / ClientSecret | Optional Google OAuth |
| Jobs__Enabled | true by default |
| AllowedHosts | Actual site hostname |

Google OAuth redirect: `https://your-host/signin-google`. Validate the external provider setup before enabling it.

## Operations

- SQL Server backups should include scheduled full/log backups and tested restore procedures.
- Monitor application logs, failed jobs, low stock/expiry notifications, POS variances and negative token review flags.
- No live third-party payment/marketplace webhook is enabled by default. Configure credentials and signed event handling before replacing the stubs.
- Do not expose IIS detailed errors or EF sensitive-data logging on Production.
