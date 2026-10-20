using System.Text.Json.Serialization;
using Battleship.Api;
using Microsoft.AspNetCore.Http.Features;
using Serilog;
using Serilog.Events;

// Composition root of the API: every service is registered here.

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// Always raise an exception for unreadable requests, so ApiExceptionMiddleware gives them the same problem details as other errors.
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<Func<Random>>(_ => () => new Random());
builder.Services.AddSingleton<GameSessionFactory>();
builder.Services.AddSingleton<GameStore>();

var logFile = Path.Combine(AppContext.BaseDirectory, "logs", "battleship-api-.log");
var serilog = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, shared: true)
    .CreateLogger();
builder.Logging.AddSerilog(serilog, dispose: true);

var app = builder.Build();

app.UseExceptionHandler();

// Answers 404, 405, and 415 from the framework with the same problem details as the errors raised by the API.
app.UseStatusCodePages();
app.UseMiddleware<ApiExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGameEndpoints();

app.Run();

/// <summary>Makes the entry point visible to the integration tests.</summary>
public partial class Program
{
}
