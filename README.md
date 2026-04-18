# QuotationAPI.V2 (.NET 8)

Standalone ASP.NET Core 8 Web API for Quotation-v2.0 frontend.

## Tech Stack
- ASP.NET Core 8 Web API
- EF Core 8 (Code-First)
- PostgreSQL (localhost for development, Supabase for production)
- JWT auth endpoints
- Swagger/OpenAPI

## Run Locally
1. Restore and build
- `dotnet restore`
- `dotnet build`

2. Run API
- `dotnet run`

3. API URL
- `http://localhost:7502`
- Swagger: `http://localhost:7502/swagger`

## Database Initialization
- Tables are created through EF Core migrations on startup (`Database.Migrate()`).
- No demo/mock seed data is inserted automatically.

## Angular Linking
In Quotation-v2.0 development environment:
- `apiUrl: 'http://localhost:7502/api'`

## Deployment
1. Publish
- `dotnet publish -c Release -o ./publish`

2. Set production config
- `Supabase:PoolerConnectionString` or `ConnectionStrings:DefaultConnection`
- `Jwt:Key`
- CORS allowed origins

3. Run on server
- `dotnet QuotationAPI.V2.dll`

## Render Deployment
1. Render service setup
- Runtime: Docker
- Root directory: `.`
- Blueprint/config: `render.yaml`
- Health check path: `/health`

Database setup guide:
- See `SUPABASE_SETUP.md` for the exact Supabase PostgreSQL setup and Render connection flow.

2. Render environment variables
- `Supabase__PoolerConnectionString`
- `SUPABASE_POOLER_CONNECTION_STRING` or `DATABASE_URL` can also be used
- `ConnectionStrings__DefaultConnection` (optional fallback outside Render)
- `Jwt__Key`
- `Jwt__Issuer=QuotationAPI.V2`
- `Jwt__Audience=QuotationApp`
- `Cors__AllowedOrigins__0=<your frontend URL>`

Example PostgreSQL connection string for Render:
- `Host=aws-0-<region>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<real-password>;SSL Mode=Require`

Do not use the direct `db.<project-ref>.supabase.co` host on Render. Use the Supabase session pooler connection instead.
If Render only injects a direct Supabase URL on port `5432`, the API automatically switches it to the Supavisor transaction pooler port `6543` during startup.

3. GitHub Actions secret
- Add repository secret `RENDER_DEPLOY_HOOK_URL`
- Do not commit the deploy hook URL into tracked files

4. Deployment flow
- Push to `main` or `master` to trigger `.github/workflows/deploy-render.yml`
- The workflow builds the API, then POSTs to the Render deploy hook
- You can also run the workflow manually from the GitHub Actions tab using `workflow_dispatch`

5. Verify after deploy
- Open `https://<your-render-service>/health`
- Expected response: `{"status":"healthy"}`

## Notes
- Keep secrets out of tracked appsettings files. Set local and deployment secrets through environment variables or user-secrets instead:
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `ZohoBooks__ClientId`
- `ZohoBooks__ClientSecret`
- `DefaultAdmin__Password` if you want bootstrap admin creation on a fresh database
- Use `dotnet ef migrations add <Name>` and `dotnet ef database update` when schema changes.
