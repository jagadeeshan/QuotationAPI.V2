# Supabase PostgreSQL Setup For QuotationAPI.V2

This API already uses PostgreSQL via EF Core and has an initial migration in the `Migrations/` folder.

You do not need to create tables manually in Supabase.
When the API starts with a valid PostgreSQL connection string, it runs EF Core migrations automatically and creates the schema.

## 1. Create Supabase Project

1. Sign in to Supabase.
2. Click `New project`.
3. Choose your organization.
4. Enter project name, for example `quotation-api-v2`.
5. Set a strong database password and save it.
6. Choose a region close to Render.
7. Wait for the database to finish provisioning.

## 2. Get The PostgreSQL Connection Details

After the project is ready:

1. Open your Supabase project.
2. Go to `Settings`.
3. Open `Database`.
4. Find the connection information.
5. Copy these values:
   - Session pooler host
   - Port
   - Database name
   - User
   - Password

Use the session pooler connection string format below for Render:

```text
Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<your-project-ref>;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true
```

Why this matters:

- Render may fail to reach the direct `db.<project-ref>.supabase.co` endpoint when it resolves to IPv6.
- The Supabase pooler endpoint is the safer production option for Render.

Do not use:

- `localhost`
- `127.0.0.1`
- `db.<your-project-ref>.supabase.co` on Render
- placeholder values such as `REPLACE_SUPABASE_HOST`

## 3. Configure Render Environment Variables

Open your Render service for `QuotationAPI.V2` and go to `Environment`.

Set these variables:

1. `Supabase__PoolerConnectionString`
   - Use the real Supabase session pooler connection string.
2. `ConnectionStrings__DefaultConnection`
   - Optional fallback for non-Render environments.
2. `Jwt__Key`
   - Use a strong secret at least 32 characters.
3. `Jwt__Issuer`
   - `QuotationAPI.V2`
4. `Jwt__Audience`
   - `QuotationApp`
5. `Cors__AllowedOrigins__0`
   - Your frontend URL, for example `https://quotation-v2.vercel.app`

Optional Zoho variables can stay empty unless those features are required.

## 4. Connect The API To Supabase Locally

If you want to test locally before deploying, update the connection string in user secrets or environment variables.

PowerShell example:

```powershell
$env:Supabase__PoolerConnectionString="Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<your-project-ref>;Password=<your-password>;SSL Mode=Require;Trust Server Certificate=true"
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run
```

Expected result:

1. API starts successfully.
2. EF Core applies the initial migration automatically.
3. The health endpoint responds at `/health`.

## 5. Verify Tables Were Created In Supabase

After the API starts successfully:

1. Open Supabase.
2. Go to `Table Editor`.
3. Confirm the API tables exist.

You should see business tables created by EF Core, including quotation, accounts, employee, admin, LOV, and related tables.

## 6. Deploy Flow

Once the connection string is correct:

1. Push this API repo to `main`.
2. GitHub Actions triggers the Render workflow.
3. Render starts the container.
4. API connects to Supabase.
5. EF Core runs migrations.
6. Render health check passes on `/health`.

## 7. Troubleshooting

### Error: `Name or service not known`

Cause:
- Invalid host name in `ConnectionStrings__DefaultConnection`

Fix:
- Replace placeholder host with the real Supabase host from `Settings -> Database`

### Error: authentication failed for user postgres

Cause:
- Wrong password in connection string

Fix:
- Reset or copy the correct database password from Supabase settings

### Error: timeout while connecting

Cause:
- Direct Supabase host on Render or temporary network issue

Fix:
- Use the Supabase session pooler host instead of `db.<project-ref>.supabase.co`
- Recopy the host exactly from Supabase
- Retry deploy

### Error: API starts locally but fails on Render

Cause:
- Render environment variable not set or not saved

Fix:
- Reopen Render environment settings and confirm `Supabase__PoolerConnectionString` is present

## 8. Minimum Working Example

Use this as the final shape, replacing the host and password only:

```text
Host=aws-0-ap-south-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.abcd1234xyz;Password=YOUR_REAL_PASSWORD;SSL Mode=Require;Trust Server Certificate=true
```