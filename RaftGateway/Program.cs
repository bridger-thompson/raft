using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using RaftShared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var nodes = Environment.GetEnvironmentVariable("NODES")?.Split(',')?.ToList() ?? [];
builder.Services.AddSingleton(serviceProvider =>
{
    var logger = serviceProvider.GetRequiredService<ILogger<Gateway>>();
    return new Gateway(nodes, logger);
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});
var serviceName = "RaftGatewayService";

builder.Logging.AddOpenTelemetry(options =>
{
    options.AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://raft-otel-collector:4317");
    }).SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


app.MapControllers();
app.Run();
