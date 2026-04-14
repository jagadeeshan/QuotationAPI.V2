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

## Notes
- Update `ConnectionStrings:DefaultConnection` in appsettings files before first run.
- Use `dotnet ef migrations add <Name>` and `dotnet ef database update` when schema changes.
