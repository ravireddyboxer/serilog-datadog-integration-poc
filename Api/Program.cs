using Microsoft.AspNetCore.HttpOverrides;
using Serilog;

// Bootstrapping serilog Logger
var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateBootstrapLogger();
// ---------------------------------------------

Log.Information("Application is starting up");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHttpContextAccessor(); // This is needed for serilog correlationId generator

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Inject serilog into asp.net core ILogger implementation
builder.Logging.ClearProviders();
builder.Host.UseSerilog((hostContext, services, configuration) =>
{
    configuration.ReadFrom.Configuration(hostContext.Configuration);
    configuration.ReadFrom.Services(services);
});
builder.Services.AddHealthChecks();
//----------------------------------------


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});
app.UsePathBase("/serilog-datadog-sampleapp");
app.MapHealthChecks("/");// this is the path used in aws load balancer
app.UseRouting();
app.UseSerilogRequestLogging();
if (app.Environment.IsProduction())
{
    app.UseHttpLogging();
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();
