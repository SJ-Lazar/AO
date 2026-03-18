using AO.API.Authentication;
using AO.Core.Features.Seed;
using AO.Core.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Serilog;

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

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

    await dbContext.Database.ExecuteSqlRawAsync(createActivitiesTableSql);
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_CompanyId\" ON \"Activities\" (\"CompanyId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_ContactId\" ON \"Activities\" (\"ContactId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_DealId\" ON \"Activities\" (\"DealId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_TaskId\" ON \"Activities\" (\"TaskId\");");
    await dbContext.Database.ExecuteSqlRawAsync("CREATE INDEX IF NOT EXISTS \"IX_Activities_CreatedUtc\" ON \"Activities\" (\"CreatedUtc\");");
}
