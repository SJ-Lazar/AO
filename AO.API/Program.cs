using AO.API.Authentication;
using AO.API.Helpers;
using AO.Core.Features.Seed;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Serilog;

try
{
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AOContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services
    .AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, _ => { });
builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AOContext>();
    dbContext.Database.EnsureCreated();
    await EnsureSqliteSchemaAsync(dbContext);
    await SeedSlice.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;
        var statusCode = exception is ArgumentException
            ? StatusCodes.Status400BadRequest
            : StatusCodes.Status500InternalServerError;
        var message = exception is ArgumentException
            ? exception.Message
            : "An unexpected server error occurred.";

        app.Logger.LogError(exception, "Unhandled API exception.");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(ResponseFactory.Failure(message, statusCode));
    });
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "AO API terminated unexpectedly.");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static async Task EnsureSqliteSchemaAsync(AOContext dbContext)
{
    if (!dbContext.Database.IsSqlite())
    {
        return;
    }

    const string createActivitiesTableSql = """
        CREATE TABLE IF NOT EXISTS "Activities" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_Activities" PRIMARY KEY,
            "Type" TEXT NOT NULL,
            "Title" TEXT NOT NULL,
            "Description" TEXT NULL,
            "CompanyId" TEXT NULL,
            "ContactId" TEXT NULL,
            "DealId" TEXT NULL,
            "TaskId" TEXT NULL,
            "CreatedUtc" TEXT NOT NULL,
            CONSTRAINT "FK_Activities_Companies_CompanyId" FOREIGN KEY ("CompanyId") REFERENCES "Companies" ("Id") ON DELETE SET NULL,
            CONSTRAINT "FK_Activities_Contacts_ContactId" FOREIGN KEY ("ContactId") REFERENCES "Contacts" ("Id") ON DELETE SET NULL,
            CONSTRAINT "FK_Activities_Deals_DealId" FOREIGN KEY ("DealId") REFERENCES "Deals" ("Id") ON DELETE SET NULL,
            CONSTRAINT "FK_Activities_Tasks_TaskId" FOREIGN KEY ("TaskId") REFERENCES "Tasks" ("Id") ON DELETE SET NULL
        );
        """;

    const string createUsersTableSql = """
        CREATE TABLE IF NOT EXISTS "Users" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY,
            "FirstName" TEXT NOT NULL,
            "LastName" TEXT NOT NULL,
            "Email" TEXT NULL,
            "IsActive" INTEGER NOT NULL,
            "CreatedUtc" TEXT NOT NULL,
            "UpdatedUtc" TEXT NOT NULL
        );
        """;

    await dbContext.Database.ExecuteSqlRawAsync(createActivitiesTableSql);
    await dbContext.Database.ExecuteSqlRawAsync(createUsersTableSql);
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_CompanyId\" ON \"Activities\" (\"CompanyId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_ContactId\" ON \"Activities\" (\"ContactId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_DealId\" ON \"Activities\" (\"DealId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_TaskId\" ON \"Activities\" (\"TaskId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_CreatedUtc\" ON \"Activities\" (\"CreatedUtc\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Users_Email\" ON \"Users\" (\"Email\");");

    await EnsureColumnAsync(dbContext, "Companies", "AssignedUserId", "TEXT NULL");
    await EnsureColumnAsync(dbContext, "Contacts", "AssignedUserId", "TEXT NULL");
    await EnsureColumnAsync(dbContext, "Deals", "AssignedUserId", "TEXT NULL");
    await EnsureColumnAsync(dbContext, "Tasks", "AssignedUserId", "TEXT NULL");

    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Companies_AssignedUserId\" ON \"Companies\" (\"AssignedUserId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Contacts_AssignedUserId\" ON \"Contacts\" (\"AssignedUserId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Deals_AssignedUserId\" ON \"Deals\" (\"AssignedUserId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Tasks_AssignedUserId\" ON \"Tasks\" (\"AssignedUserId\");");
}

static async Task EnsureColumnAsync(AOContext dbContext, string tableName, string columnName, string columnDefinition)
{
    if (await ColumnExistsAsync(dbContext, tableName, columnName))
    {
        return;
    }

    await dbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnDefinition};");
}

static async Task<bool> ColumnExistsAsync(AOContext dbContext, string tableName, string columnName)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != System.Data.ConnectionState.Open;

    if (shouldClose)
    {
        await connection.OpenAsync();
    }

    try
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{tableName}\");";

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            if (string.Equals(reader[1]?.ToString(), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
    finally
    {
        if (shouldClose)
        {
            await connection.CloseAsync();
        }
    }
}
