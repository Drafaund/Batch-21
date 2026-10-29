using System.Text.Json.Serialization;
using Battleship.Api;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)))
    .ConfigureApiBehaviorOptions(options => options.InvalidModelStateResponseFactory = InvalidRequestResponse.Create);
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSignalR().AddJsonProtocol(options =>
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

builder.Services.AddSingleton<Func<Random>>(_ => () => new Random());
builder.Services.AddSingleton(builder.Configuration.GetSection("Game").Get<GameOptions>() ?? new GameOptions());
builder.Services.AddSingleton<IGameEventSink, HubGameEventSink>();
builder.Services.AddSingleton<GameSessionFactory>();
builder.Services.AddSingleton<GameStore>();

// The static Log.Logger is left alone: every host (and every test host) keeps its own logger.
// FromLogContext is what carries the ASP.NET request scope (RequestId, RequestPath, ...) onto entries written straight through Serilog.
builder.Host.UseSerilog(
    (context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext(),
    preserveStaticLogger: true);

var app = builder.Build();

app.UseSerilogRequestLogging(options => options.Logger = app.Services.GetRequiredService<Serilog.ILogger>());

app.UseExceptionHandler();

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
