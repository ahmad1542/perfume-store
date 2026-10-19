using PerfumeStore.Application.Extensions;
using PerfumeStore.Infrastructure.Extensions;
using PerfumeStore.API.Errors;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using PerfumeStore.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddControllers();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails(options => {
    options.CustomizeProblemDetails = context => {
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    };
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var allowedOrigins = "_allowedOrigins";

builder.Services.AddCors(options => {
    options.AddPolicy(name: allowedOrigins, policy => {
        policy
            .WithOrigins("http://127.0.0.1:5500", "http://localhost:5500")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
if (builder.Configuration.GetValue("Database:Initialize", false)) {
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<PerfumeStoreDbContext>();
    await database.Database.MigrateAsync();
    await database.InitializeFinancialHistoryAsync();
}
app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (builder.Configuration.GetValue("Hosting:UseHttpsRedirection", true)) {
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(allowedOrigins);

app.UseAuthorization();

app.Use(async (context, next) => {
    if (context.Request.Path.StartsWithSegments("/api") && context.Request.Method is "POST" or "PATCH" or "PUT" or "DELETE") {
        var parts = context.Request.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var database = context.RequestServices.GetRequiredService<PerfumeStoreDbContext>();
        database.HistoryContext.Resource = parts.ElementAtOrDefault(1) ?? "";
        database.HistoryContext.SourceId = parts.ElementAtOrDefault(2) ?? "";
        database.HistoryContext.Action = context.Request.Method == "POST" ? "Created" : "Corrected";
    }
    await next();
});

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/Dashboard/index.html"));
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

public partial class Program { }
