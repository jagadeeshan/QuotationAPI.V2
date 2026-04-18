using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.LOV;
using QuotationAPI.V2.Services;

var builder = WebApplication.CreateBuilder(args);
var isProduction = builder.Environment.IsProduction();
var isProdFree = builder.Environment.IsEnvironment("ProdFree");
var isProductionLike = isProduction || isProdFree;
var databaseProvider = ResolveDatabaseProvider(builder.Configuration);

var resolvedDatabaseConnection = ResolveDatabaseConnection(builder.Configuration, builder.Environment, databaseProvider);
var connectionValidation = ValidateDatabaseConnection(
    resolvedDatabaseConnection.ConnectionString,
    databaseProvider,
    isProductionLike,
    IsRenderEnvironment());
var isDatabaseConfigured = connectionValidation.IsValid;

if (!isDatabaseConfigured)
{
    // Log validation errors at startup but do NOT crash — allows health check to pass so Render deploy succeeds.
    var earlyLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    earlyLogger.LogCritical(
        "DATABASE NOT CONFIGURED for provider {Provider}: {Error}. The API will start in degraded mode (health check OK, DB endpoints will fail)."
        + " For PostgreSQL on Render, set Supabase__PoolerConnectionString and escape '$' as '$$' if present.",
        databaseProvider,
        connectionValidation.Error);
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register DbContext: use real connection when available, dummy placeholder when not.
// The placeholder lets DI resolve controllers without crashing at startup.
var dbConnectionString = isDatabaseConfigured
    ? resolvedDatabaseConnection.ConnectionString!
    : IsSqlServerProvider(databaseProvider)
        ? "Server=unconfigured;Database=none;User Id=none;Password=none;TrustServerCertificate=True"
        : "Host=unconfigured;Database=none;Username=none;Password=none";
builder.Services.AddDbContext<QuotationDbContext>(options =>
{
    if (IsSqlServerProvider(databaseProvider))
    {
        options.UseSqlServer(dbConnectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null));
    }
    else
    {
        options.UseNpgsql(dbConnectionString);
    }
});

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IZohoBooksService, ZohoBooksService>();

var isDevelopment = builder.Environment.IsDevelopment();
var configuredJwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    if (isProductionLike)
    {
        throw new InvalidOperationException("Jwt:Key must be configured in non-development environments.");
    }

    configuredJwtKey = "DEV_ONLY_SUPER_SECRET_KEY_CHANGE_ME_123456";
}

if (isProductionLike && configuredJwtKey.StartsWith("DEV_ONLY_", StringComparison.OrdinalIgnoreCase))
{
    throw new InvalidOperationException("Refusing to start with development JWT key outside development.");
}

var jwtKey = configuredJwtKey;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuotationAPI.V2";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QuotationApp";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularCors", policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var configuredOriginPatterns = builder.Configuration.GetSection("Cors:AllowedOriginPatterns").Get<string[]>() ?? Array.Empty<string>();

        var defaultOrigins = new[]
        {
            "http://localhost:4200",
            "http://localhost:4210",
            "https://magizhops.vercel.app",
        };

        var allowedOrigins = configuredOrigins.Length > 0 ? configuredOrigins : defaultOrigins;

        var defaultOriginPatterns = new[]
        {
            "https://*.vercel.app"
        };
        var allowedOriginPatterns = configuredOriginPatterns.Length > 0 ? configuredOriginPatterns : defaultOriginPatterns;

        policy.SetIsOriginAllowed(origin => IsCorsOriginAllowed(origin, allowedOrigins, allowedOriginPatterns))
            .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS");
    });
});

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
if (!string.IsNullOrWhiteSpace(resolvedDatabaseConnection.ConnectionString))
{
    if (IsSqlServerProvider(databaseProvider))
    {
        var connectionInfo = new SqlConnectionStringBuilder(resolvedDatabaseConnection.ConnectionString);
        startupLogger.LogInformation(
            "Using SQL Server connection source: {ConnectionSource}; DataSource: {DataSource}; InitialCatalog: {InitialCatalog}",
            resolvedDatabaseConnection.Source,
            connectionInfo.DataSource,
            connectionInfo.InitialCatalog);
    }
    else
    {
        var connectionInfo = new NpgsqlConnectionStringBuilder(resolvedDatabaseConnection.ConnectionString);
        startupLogger.LogInformation(
            "Using PostgreSQL connection source: {ConnectionSource}; Host: {Host}; Port: {Port}; SSL: {SslMode}; MaxPool: {MaxPool}",
            resolvedDatabaseConnection.Source,
            connectionInfo.Host,
            connectionInfo.Port,
            connectionInfo.SslMode,
            connectionInfo.MaxPoolSize);

        // Warn if the resolved host looks suspicious (empty, truncated, or has leftover env var syntax).
        if (string.IsNullOrWhiteSpace(connectionInfo.Host) || connectionInfo.Host.Contains("$"))
        {
            startupLogger.LogError(
                "SUSPICIOUS HOST DETECTED: '{Host}'. This may indicate Render expanded a $VARIABLE in the connection string env var. "
                + "If your Supabase password contains '$', escape it as '$$' in Render environment variables.",
                connectionInfo.Host);
        }
    }
}
else
{
    startupLogger.LogWarning("No {DatabaseProvider} connection string resolved. Source: {ConnectionSource}", databaseProvider, resolvedDatabaseConnection.Source);
}

// Swagger enabled in all environments for API testing.
app.UseSwagger();
app.UseSwaggerUI();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                message = "An unexpected server error occurred.",
                status = 500,
                traceId = context.TraceIdentifier,
            }));
        });
    });

    app.UseHsts();
    app.UseHttpsRedirection();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
    context.Response.Headers.TryAdd("Referrer-Policy", "no-referrer");

    await next();
});

app.UseCors("AngularCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/", () => Results.Ok(new
{
    status = "ok",
    service = "QuotationAPI.V2",
    provider = databaseProvider,
    database = isDatabaseConfigured ? "configured" : "NOT_CONFIGURED"
}));
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    provider = databaseProvider,
    database = isDatabaseConfigured ? "configured" : "NOT_CONFIGURED",
    migrationStatus = MigrationTracker.Status,
    lastMigrationError = MigrationTracker.Error,
    lastUpdatedUtc = MigrationTracker.LastUpdatedUtc
}));
app.MapControllers();

// Run migrations and seed data in background so the /health endpoint responds
// immediately and Render does not cancel the deploy due to health check timeout.
if (!isDatabaseConfigured)
{
    MigrationTracker.Status = "skipped";
    MigrationTracker.Error = "No database connection configured";
    MigrationTracker.LastUpdatedUtc = DateTime.UtcNow;
    
    var noDB = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
    noDB.LogWarning(
        "Skipping database migration — no valid connection string configured. "
        + "Set Supabase__PoolerConnectionString in Render environment variables, then redeploy.");
}
else
{
_ = Task.Run(async () =>
{
    // Small delay to let Kestrel bind and start listening first.
    await Task.Delay(TimeSpan.FromSeconds(2));

    const int maxRetries = 5;
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        MigrationTracker.Status = "running";
        MigrationTracker.LastUpdatedUtc = DateTime.UtcNow;
        
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QuotationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

            logger.LogInformation("Database migration attempt {Attempt}/{MaxRetries}...", attempt, maxRetries);

            await db.Database.MigrateAsync();

            logger.LogInformation("Database migration completed successfully.");

            await EnsureRefreshTokenTableAsync(db);
            await EnsurePriceLovDefaultsAsync(db);
            await EnsureExpenseCategoryLovDefaultsAsync(db);
            await EnsureZohoBooksTablesAsync(db);

            logger.LogInformation("Database seeding completed successfully.");
            
            // Update status on success
            MigrationTracker.Status = "succeeded";
            MigrationTracker.Error = null;
            MigrationTracker.LastUpdatedUtc = DateTime.UtcNow;
            return; // Success — exit retry loop.
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");

            string host = "unknown";
            int port = 0;
            if (!string.IsNullOrWhiteSpace(resolvedDatabaseConnection.ConnectionString))
            {
                try
                {
                    if (IsSqlServerProvider(databaseProvider))
                    {
                        var parsed = new SqlConnectionStringBuilder(resolvedDatabaseConnection.ConnectionString);
                        host = NormalizeDbHost(parsed.DataSource);
                        port = 1433;
                    }
                    else
                    {
                        var parsed = new NpgsqlConnectionStringBuilder(resolvedDatabaseConnection.ConnectionString);
                        host = NormalizeDbHost(parsed.Host);
                        port = parsed.Port;
                    }
                }
                catch
                {
                    host = "invalid-connection-string";
                }
            }

            if (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // Exponential backoff: 2, 4, 8, 16, 32s
                logger.LogWarning(ex,
                    "Database migration attempt {Attempt}/{MaxRetries} failed. Source: {ConnectionSource}; Host: {Host}; Port: {Port}. Retrying in {Delay}s...",
                    attempt, maxRetries, resolvedDatabaseConnection.Source, host, port, delay.TotalSeconds);
                await Task.Delay(delay);
            }
            else
            {
                // Update status on final failure
                MigrationTracker.Status = "failed";
                MigrationTracker.Error = $"{ex.GetType().Name}: {ex.Message}";
                MigrationTracker.LastUpdatedUtc = DateTime.UtcNow;
                
                logger.LogCritical(ex,
                    "Database migration failed after {MaxRetries} attempts. Source: {ConnectionSource}; Host: {Host}; Port: {Port}. "
                    + "The API will run but database may be in an inconsistent state. "
                    + "Verify the env var Supabase__PoolerConnectionString is set correctly on Render. "
                    + "IMPORTANT: If password contains '$', escape it as '$$' in Render env vars (Render expands $VAR references).",
                    maxRetries, resolvedDatabaseConnection.Source, host, port);
            }
        }
    }
});
} // end else (isDatabaseConfigured)

app.Run();

static string ResolveDatabaseProvider(IConfiguration configuration)
{
    var configuredProvider =
        Environment.GetEnvironmentVariable("DB_PROVIDER") ??
        configuration["Database:Provider"] ??
        "PostgreSql";

    if (configuredProvider.Equals("sqlserver", StringComparison.OrdinalIgnoreCase) ||
        configuredProvider.Equals("mssql", StringComparison.OrdinalIgnoreCase))
    {
        return "SqlServer";
    }

    return "PostgreSql";
}

static bool IsSqlServerProvider(string provider)
{
    return provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase);
}

static bool IsCorsOriginAllowed(string origin, IEnumerable<string> allowedOrigins, IEnumerable<string> allowedOriginPatterns)
{
    if (string.IsNullOrWhiteSpace(origin))
    {
        return false;
    }

    if (allowedOrigins.Any(o => string.Equals(o, origin, StringComparison.OrdinalIgnoreCase)))
    {
        return true;
    }

    return allowedOriginPatterns.Any(pattern => IsOriginPatternMatch(origin, pattern));
}

static bool IsOriginPatternMatch(string origin, string pattern)
{
    if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(pattern))
    {
        return false;
    }

    if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
    {
        return false;
    }

    if (!Uri.TryCreate(pattern.Replace("*.", "wildcard."), UriKind.Absolute, out var patternUri))
    {
        return false;
    }

    var patternHost = patternUri.Host.Replace("wildcard.", "", StringComparison.OrdinalIgnoreCase);
    var expectedSuffix = "." + patternHost;

    if (!string.Equals(originUri.Scheme, patternUri.Scheme, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    return originUri.Host.EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase);
}

static (string? ConnectionString, string Source) ResolveDatabaseConnection(
    IConfiguration configuration,
    IHostEnvironment environment,
    string provider)
{
    return IsSqlServerProvider(provider)
        ? ResolveSqlServerConnection(configuration)
        : ResolveDefaultConnection(configuration, environment);
}

static (bool IsValid, string? Error) ValidateDatabaseConnection(
    string? connectionString,
    string provider,
    bool isProductionLike,
    bool isRenderEnvironment)
{
    return IsSqlServerProvider(provider)
        ? ValidateSqlServerConnection(connectionString, isProductionLike)
        : ValidateDefaultConnection(connectionString, isProductionLike, isRenderEnvironment);
}

static (string? ConnectionString, string Source) ResolveSqlServerConnection(IConfiguration configuration)
{
    var candidates = new (string Source, string? Value)[]
    {
        ("ConnectionStrings__SqlServerConnection", Environment.GetEnvironmentVariable("ConnectionStrings__SqlServerConnection")),
        ("ConnectionStrings__DefaultConnection", Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")),
        ("SQLSERVER_CONNECTION_STRING", Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING")),
        ("AZURE_SQL_CONNECTION_STRING", Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTION_STRING")),
        ("ConnectionStrings:SqlServerConnection", configuration.GetConnectionString("SqlServerConnection")),
        ("ConnectionStrings:DefaultConnection", configuration.GetConnectionString("DefaultConnection"))
    };

    var selected = candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate.Value));
    return string.IsNullOrWhiteSpace(selected.Value)
        ? (null, "none")
        : (selected.Value, selected.Source);
}

static (bool IsValid, string? Error) ValidateSqlServerConnection(string? connectionString, bool isProductionLike)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return (false, "No SQL Server connection string is configured. Set ConnectionStrings__SqlServerConnection or AZURE_SQL_CONNECTION_STRING.");
    }

    SqlConnectionStringBuilder builder;
    try
    {
        builder = new SqlConnectionStringBuilder(connectionString);
    }
    catch (Exception ex)
    {
        return (false, $"The configured SQL Server connection string is not valid: {ex.Message}");
    }

    if (string.IsNullOrWhiteSpace(builder.DataSource))
    {
        return (false, "The configured SQL Server connection string must include Data Source/Server.");
    }

    if (isProductionLike)
    {
        if (builder.DataSource.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Production-like SQL Server host is still a placeholder.");
        }

        if (string.Equals(builder.DataSource, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.DataSource, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Production-like SQL Server host cannot be localhost.");
        }
    }

    return (true, null);
}

static (string? ConnectionString, string Source) ResolveDefaultConnection(IConfiguration configuration, IHostEnvironment environment)
{
    var isProductionLike = IsProductionLike(environment);

    var candidates = new (string Source, string? Value)[]
    {
        ("Supabase__PoolerConnectionString", Environment.GetEnvironmentVariable("Supabase__PoolerConnectionString")),
        ("SUPABASE_POOLER_CONNECTION_STRING", Environment.GetEnvironmentVariable("SUPABASE_POOLER_CONNECTION_STRING")),
        ("ConnectionStrings__DefaultConnection", Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")),
        ("Supabase:PoolerConnectionString", configuration["Supabase:PoolerConnectionString"]),
        ("ConnectionStrings:DefaultConnection", configuration.GetConnectionString("DefaultConnection")),
        ("DATABASE_URL", Environment.GetEnvironmentVariable("DATABASE_URL")),
        ("POSTGRES_URL", Environment.GetEnvironmentVariable("POSTGRES_URL")),
        ("POSTGRES_PRISMA_URL", Environment.GetEnvironmentVariable("POSTGRES_PRISMA_URL")),
        ("SUPABASE_DB_URL", Environment.GetEnvironmentVariable("SUPABASE_DB_URL"))
    };

    var selected = isProductionLike
        ? candidates.FirstOrDefault(candidate => IsViableProductionConnectionCandidate(candidate.Value))
        : candidates.FirstOrDefault(candidate =>
            candidate.Source is "ConnectionStrings:DefaultConnection" or "ConnectionStrings__DefaultConnection"
            && !string.IsNullOrWhiteSpace(candidate.Value));

    var resolvedConnection = selected.Value;

    if (string.IsNullOrWhiteSpace(resolvedConnection))
    {
        return (null, "none");
    }

    resolvedConnection = NormalizePostgreSqlConnectionString(resolvedConnection);

    var builder = new NpgsqlConnectionStringBuilder(resolvedConnection);
    builder.Host = NormalizeDbHost(builder.Host);

    if (isProductionLike &&
        IsRenderEnvironment() &&
        IsSupabaseDirectStyleHost(builder.Host) &&
        builder.Port == 5432)
    {
        builder.Port = 6543;
        selected.Source = $"{selected.Source} (auto-switched-to-supavisor-transaction-port)";
    }

    if (isProductionLike)
    {
        if (builder.SslMode == SslMode.Disable || builder.SslMode == SslMode.Prefer)
        {
            builder.SslMode = SslMode.Require;
        }

        // Per Supabase connection management best practices:
        // Supavisor session pooler requires Npgsql-side pooling to be configured properly.
        builder.Timeout = builder.Timeout > 0 ? builder.Timeout : 15;
        builder.CommandTimeout = builder.CommandTimeout > 0 ? builder.CommandTimeout : 60;
        builder.KeepAlive = builder.KeepAlive > 0 ? builder.KeepAlive : 30;

        // Limit pool size to avoid overwhelming Supabase free-tier connection limits.
        if (builder.MaxPoolSize <= 0 || builder.MaxPoolSize > 20)
        {
            builder.MaxPoolSize = 20;
        }
        builder.MinPoolSize = 0;

        // Disable Npgsql multiplexing — incompatible with Supavisor transaction pooler.
        builder.Multiplexing = false;
    }

    return (builder.ConnectionString, selected.Source);
}

static (bool IsValid, string? Error) ValidateDefaultConnection(string? connectionString, bool isProduction, bool isRenderEnvironment)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return (false, "No PostgreSQL connection string is configured. Set Supabase__PoolerConnectionString for Render or ConnectionStrings__DefaultConnection for other environments.");
    }

    NpgsqlConnectionStringBuilder builder;
    try
    {
        builder = new NpgsqlConnectionStringBuilder(connectionString);
    }
    catch (Exception ex)
    {
        return (false, $"The configured PostgreSQL connection string is not valid: {ex.Message}");
    }

    if (string.IsNullOrWhiteSpace(builder.Host))
    {
        return (false, "The configured PostgreSQL connection string must include a PostgreSQL host.");
    }

    if (isProduction)
    {
        builder.Host = NormalizeDbHost(builder.Host);

        if (builder.Host.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Production database host is still a placeholder. Set Supabase__PoolerConnectionString in Render with your real Supabase pooler host.");
        }

        if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Production database host cannot be localhost. Set Supabase__PoolerConnectionString in Render or ConnectionStrings__DefaultConnection to your external PostgreSQL host.");
        }

        if (string.IsNullOrWhiteSpace(builder.Password) ||
            builder.Password.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Production database password is missing or still a placeholder. Set Supabase__PoolerConnectionString in Render with the real credentials.");
        }

        if (isRenderEnvironment &&
            IsSupabaseDirectStyleHost(builder.Host) &&
            builder.Port == 5432)
        {
            return (false, "Render cannot reliably use the direct Supabase host on port 5432. Use a Supabase pooler connection string or let the app use the Supavisor transaction pooler on port 6543.");
        }
    }

    return (true, null);
}

static bool IsRenderEnvironment()
{
    return !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER")) ||
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER_SERVICE_ID")) ||
           !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL"));
}

static bool IsProductionLike(IHostEnvironment environment)
{
    return environment.IsProduction() || environment.IsEnvironment("ProdFree");
}

static bool IsViableProductionConnectionCandidate(string? connectionString)
{
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return false;
    }

    var normalized = NormalizePostgreSqlConnectionString(connectionString);

    try
    {
        var builder = new NpgsqlConnectionStringBuilder(normalized);
        builder.Host = NormalizeDbHost(builder.Host);

        if (string.IsNullOrWhiteSpace(builder.Host))
        {
            return false;
        }

        if (builder.Host.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(builder.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(builder.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(builder.Password) ||
            builder.Password.Contains("REPLACE_", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
    catch
    {
        return false;
    }
}

static bool IsSupabaseDirectStyleHost(string? host)
{
    host = NormalizeDbHost(host);

    return !string.IsNullOrWhiteSpace(host) &&
           host.StartsWith("db.", StringComparison.OrdinalIgnoreCase) &&
           host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase);
}

static string NormalizeDbHost(string? host)
{
    return (host ?? string.Empty)
        .Trim()
        .Trim('"')
        .Trim('\'');
}

static string NormalizePostgreSqlConnectionString(string connectionString)
{
    if (connectionString.Contains("Host=", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
    {
        return connectionString;
    }

    if (!uri.Scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase) &&
        !uri.Scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase))
    {
        return connectionString;
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(0) ?? string.Empty);
    var password = Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty);
    var database = uri.AbsolutePath.Trim('/');
    var queryParameters = ParseQueryString(uri.Query);

    var builder = new NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.Port > 0 ? uri.Port : 5432,
        Database = string.IsNullOrWhiteSpace(database) ? "postgres" : Uri.UnescapeDataString(database),
        Username = username,
        Password = password
    };

    if (queryParameters.TryGetValue("sslmode", out var sslModeValue) &&
        Enum.TryParse<SslMode>(sslModeValue, true, out var sslMode))
    {
        builder.SslMode = sslMode;
    }

    return builder.ConnectionString;
}

static Dictionary<string, string> ParseQueryString(string queryString)
{
    var parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(queryString))
    {
        return parameters;
    }

    foreach (var pair in queryString.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
    {
        var keyValue = pair.Split('=', 2);
        var key = Uri.UnescapeDataString(keyValue[0]).Replace('+', ' ');
        var value = keyValue.Length > 1 ? Uri.UnescapeDataString(keyValue[1]).Replace('+', ' ') : string.Empty;
        parameters[key] = value;
    }

    return parameters;
}

static async Task EnsurePriceLovDefaultsAsync(QuotationDbContext db)
{
    var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    var parent = await db.LovItems
        .FirstOrDefaultAsync(item => item.Parentvalue == null && item.Name.ToLower() == "price");

    if (parent == null)
    {
        parent = new LovItem
        {
            Parentname = "",
            Parentvalue = null,
            Name = "Price",
            Value = null,
            Description = "Legacy quotation price constants",
            Itemtype = "CATEGORY",
            Displayorder = 999,
            Isactive = "Y",
            Createdby = "system",
            Updatedby = "system",
            Createddt = now,
            Updateddt = now,
        };

        db.LovItems.Add(parent);
        await db.SaveChangesAsync();
    }

    var defaults = new (string Name, int Value, string Description)[]
    {
        ("paper", 58, "Paper rate"),
        ("duplex", 72, "Duplex rate"),
        ("eb", 3200, "EB rate"),
        ("pin", 940, "Pin rate"),
        ("gum", 610, "Gum rate"),
        ("salary", 850, "Salary per shift"),
        ("rent", 12000, "Monthly rent"),
        ("transport", 0, "Transport reference value"),
    };

    foreach (var item in defaults)
    {
        var existing = await db.LovItems.FirstOrDefaultAsync(lov =>
            lov.Parentvalue == parent.Id && lov.Name.ToLower() == item.Name);

        if (existing == null)
        {
            db.LovItems.Add(new LovItem
            {
                Parentname = parent.Name,
                Parentvalue = parent.Id,
                Name = item.Name,
                Value = item.Value,
                Description = item.Description,
                Itemtype = "CATEGORY_VALUE",
                Displayorder = 1,
                Isactive = "Y",
                Createdby = "system",
                Updatedby = "system",
                Createddt = now,
                Updateddt = now,
            });
        }
        else if (!existing.Value.HasValue || existing.Value.Value <= 0)
        {
            existing.Value = item.Value;
            existing.Updatedby = "system";
            existing.Updateddt = now;
        }
    }

    await db.SaveChangesAsync();
}

static async Task EnsureExpenseCategoryLovDefaultsAsync(QuotationDbContext db)
{
    var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

    var parent = await db.LovItems
        .FirstOrDefaultAsync(item => item.Parentvalue == null && item.Name.ToLower() == "expense category");

    if (parent == null)
    {
        parent = new LovItem
        {
            Parentname = "",
            Parentvalue = null,
            Name = "Expense Category",
            Value = null,
            Description = "Business expense categories",
            Itemtype = "CATEGORY",
            Displayorder = 1000,
            Isactive = "Y",
            Createdby = "system",
            Updatedby = "system",
            Createddt = now,
            Updateddt = now,
        };

        db.LovItems.Add(parent);
        await db.SaveChangesAsync();
    }

    var defaults = new (string Name, int Value, string Description)[]
    {
        ("Transport", 4, "Transport and logistics"),
        ("Rent", 5, "Factory / office rent"),
        ("Gum", 9, "Gum purchase / usage"),
        ("Pin", 10, "Pin purchase / usage"),
        ("EB", 11, "Electricity / EB charges"),
    };

    foreach (var item in defaults)
    {
        var existing = await db.LovItems.FirstOrDefaultAsync(lov =>
            lov.Parentvalue == parent.Id && lov.Name.ToLower() == item.Name.ToLower());

        if (existing == null)
        {
            db.LovItems.Add(new LovItem
            {
                Parentname = parent.Name,
                Parentvalue = parent.Id,
                Name = item.Name,
                Value = item.Value,
                Description = item.Description,
                Itemtype = "CATEGORY_VALUE",
                Displayorder = item.Value,
                Isactive = "Y",
                Createdby = "system",
                Updatedby = "system",
                Createddt = now,
                Updateddt = now,
            });
        }
        else
        {
            existing.Description = string.IsNullOrWhiteSpace(existing.Description) ? item.Description : existing.Description;
            if (!existing.Value.HasValue || existing.Value.Value <= 0)
            {
                existing.Value = item.Value;
            }
            existing.Updatedby = "system";
            existing.Updateddt = now;
        }
    }

    await db.SaveChangesAsync();
}

static async Task EnsureZohoBooksTablesAsync(QuotationDbContext db)
{
    var sql = db.Database.IsNpgsql()
        ? """
CREATE TABLE IF NOT EXISTS "ZohoCustomerRecords" (
    "Id" VARCHAR(450) NOT NULL,
    "CustomerName" TEXT NOT NULL,
    "Email" TEXT NULL,
    "Phone" TEXT NULL,
    "OutstandingAmount" NUMERIC(18,4) NOT NULL,
    "LastModifiedTimeUtc" TIMESTAMPTZ NULL,
    "PulledAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "PK_ZohoCustomerRecords" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ZohoInvoiceRecords" (
    "Id" VARCHAR(450) NOT NULL,
    "CustomerId" TEXT NULL,
    "CustomerName" TEXT NULL,
    "InvoiceNumber" TEXT NULL,
    "InvoiceDate" TIMESTAMPTZ NULL,
    "DueDate" TIMESTAMPTZ NULL,
    "Total" NUMERIC(18,4) NOT NULL,
    "Balance" NUMERIC(18,4) NOT NULL,
    "Status" TEXT NULL,
    "LastModifiedTimeUtc" TIMESTAMPTZ NULL,
    "PulledAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "PK_ZohoInvoiceRecords" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ZohoOutstandingRecords" (
    "Id" VARCHAR(450) NOT NULL,
    "CustomerName" TEXT NULL,
    "OutstandingAmount" NUMERIC(18,4) NOT NULL,
    "PulledAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "PK_ZohoOutstandingRecords" PRIMARY KEY ("Id")
);

CREATE TABLE IF NOT EXISTS "ZohoSyncStates" (
    "Id" INTEGER NOT NULL,
    "LastCustomersSyncUtc" TIMESTAMPTZ NULL,
    "LastInvoicesSyncUtc" TIMESTAMPTZ NULL,
    "LastOutstandingSyncUtc" TIMESTAMPTZ NULL,
    "UpdatedAtUtc" TIMESTAMPTZ NOT NULL,
    CONSTRAINT "PK_ZohoSyncStates" PRIMARY KEY ("Id")
);
"""
        : @"
IF OBJECT_ID(N'[ZohoCustomerRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoCustomerRecords] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NULL,
        [Phone] nvarchar(max) NULL,
        [OutstandingAmount] decimal(18,4) NOT NULL,
        [LastModifiedTimeUtc] datetime2 NULL,
        [PulledAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoCustomerRecords] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[ZohoInvoiceRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoInvoiceRecords] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerId] nvarchar(max) NULL,
        [CustomerName] nvarchar(max) NULL,
        [InvoiceNumber] nvarchar(max) NULL,
        [InvoiceDate] datetime2 NULL,
        [DueDate] datetime2 NULL,
        [Total] decimal(18,4) NOT NULL,
        [Balance] decimal(18,4) NOT NULL,
        [Status] nvarchar(max) NULL,
        [LastModifiedTimeUtc] datetime2 NULL,
        [PulledAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoInvoiceRecords] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[ZohoOutstandingRecords]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoOutstandingRecords] (
        [Id] nvarchar(450) NOT NULL,
        [CustomerName] nvarchar(max) NULL,
        [OutstandingAmount] decimal(18,4) NOT NULL,
        [PulledAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoOutstandingRecords] PRIMARY KEY ([Id])
    );
END;

IF OBJECT_ID(N'[ZohoSyncStates]', N'U') IS NULL
BEGIN
    CREATE TABLE [ZohoSyncStates] (
        [Id] int NOT NULL,
        [LastCustomersSyncUtc] datetime2 NULL,
        [LastInvoicesSyncUtc] datetime2 NULL,
        [LastOutstandingSyncUtc] datetime2 NULL,
        [UpdatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ZohoSyncStates] PRIMARY KEY ([Id])
    );
END;
";

    await db.Database.ExecuteSqlRawAsync(sql);
}

static async Task EnsureRefreshTokenTableAsync(QuotationDbContext db)
{
    var providerName = db.Database.ProviderName ?? string.Empty;
    var sql = providerName.Contains("SqlServer", StringComparison.OrdinalIgnoreCase)
        ? @"
IF OBJECT_ID(N'[RefreshTokens]', N'U') IS NULL
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] nvarchar(450) NOT NULL,
        [UserId] nvarchar(450) NOT NULL,
        [TokenId] nvarchar(128) NOT NULL,
        [TokenHash] nvarchar(512) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [LastUsedAt] datetime2 NULL,
        [RevokedAt] datetime2 NULL,
        [ReplacedByTokenId] nvarchar(128) NULL,
        [RevokedReason] nvarchar(64) NULL,
        [CreatedByIp] nvarchar(256) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX [IX_RefreshTokens_TokenId] ON [RefreshTokens] ([TokenId]);
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END"
        : @"
CREATE TABLE IF NOT EXISTS ""RefreshTokens"" (
    ""Id"" text PRIMARY KEY,
    ""UserId"" text NOT NULL REFERENCES ""Users""(""Id"") ON DELETE CASCADE,
    ""TokenId"" character varying(128) NOT NULL,
    ""TokenHash"" character varying(512) NOT NULL,
    ""CreatedAt"" timestamp with time zone NOT NULL,
    ""ExpiresAt"" timestamp with time zone NOT NULL,
    ""LastUsedAt"" timestamp with time zone NULL,
    ""RevokedAt"" timestamp with time zone NULL,
    ""ReplacedByTokenId"" character varying(128) NULL,
    ""RevokedReason"" character varying(64) NULL,
    ""CreatedByIp"" character varying(256) NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_RefreshTokens_TokenId"" ON ""RefreshTokens"" (""TokenId"");
CREATE INDEX IF NOT EXISTS ""IX_RefreshTokens_UserId"" ON ""RefreshTokens"" (""UserId"");";

    await db.Database.ExecuteSqlRawAsync(sql);
}


// Migration status tracking for health endpoint observability
public static class MigrationTracker
{
    public static string Status { get; set; } = "pending";
    public static string? Error { get; set; }
    public static DateTime? LastUpdatedUtc { get; set; }
}
