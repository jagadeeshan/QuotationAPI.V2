# QuotationAPI.V2 (.NET 8)

Standalone ASP.NET Core 8 Web API for Quotation-v2.0 frontend.

## Tech Stack
- ASP.NET Core 8 Web API
- EF Core 8 (Code-First)
- SQL Server (development/production)
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
- `ConnectionStrings:DefaultConnection`
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

2. Render environment variables
- `ConnectionStrings__DefaultConnection`
- `Jwt__Key`
- `Jwt__Issuer=QuotationAPI.V2`
- `Jwt__Audience=QuotationApp`
- `Cors__AllowedOrigins__0=<your frontend URL>`

Example PostgreSQL connection string for Render:
- `Host=db.example.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=<real-password>;SSL Mode=Require;Trust Server Certificate=true`

Do not use placeholders like `REPLACE_SUPABASE_HOST` or production `localhost` values.

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
- Update `ConnectionStrings:DefaultConnection` in appsettings files before first run.
- Use `dotnet ef migrations add <Name>` and `dotnet ef database update` when schema changes.
