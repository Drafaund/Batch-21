using System.Text.Json.Serialization;
using Battleship.Api;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Serilog;
using Serilog.Events;

// Composition root of the API: every service is registered here.

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)))
    // An unreadable request body gets the same problem details, and the same log line, as the errors a Result carries.
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidRequestResponse.Create);
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddSingleton<Func<Random>>(_ => () => new Random());
// The timeouts come from the "Game" section of appsettings.json (or the environment, for example Game__TurnTimeout=00:00:20).
builder.Services.AddSingleton(builder.Configuration.GetSection("Game").Get<GameOptions>() ?? new GameOptions());
builder.Services.AddSingleton<IGameEventSink, HubGameEventSink>();
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

// Answers 404, 405, and 415 from the framework with the same problem details as the errors a Result carries.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHubAuthentication();

app.MapControllers();
app.MapHub<GameHub>(GameHub.Path);

app.Run();

public partial class Program
{
}
