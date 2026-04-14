using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotationAPI.V2.Data;
using QuotationAPI.V2.Models.LOV;
using QuotationAPI.V2.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<QuotationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IZohoBooksService, ZohoBooksService>();

var isDevelopment = builder.Environment.IsDevelopment();
var configuredJwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(configuredJwtKey))
{
    if (!isDevelopment)
    {
        throw new InvalidOperationException("Jwt:Key must be configured in non-development environments.");
    }

    configuredJwtKey = "DEV_ONLY_SUPER_SECRET_KEY_CHANGE_ME_123456";
}

if (!isDevelopment && configuredJwtKey.StartsWith("DEV_ONLY_", StringComparison.OrdinalIgnoreCase))
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
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? new[]
            {
                "http://localhost",
                "http://localhost:7500",
                "http://localhost:7501",
                "http://localhost:7502",
                "http://localhost:4200",
                "http://localhost:4210"
            };

        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
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
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuotationDbContext>();
    await db.Database.MigrateAsync();
    await EnsurePriceLovDefaultsAsync(db);
    await EnsureExpenseCategoryLovDefaultsAsync(db);
    await EnsureZohoBooksTablesAsync(db);
}

app.Run();

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
