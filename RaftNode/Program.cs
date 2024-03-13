using RaftShared;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient();

var nodes = Environment.GetEnvironmentVariable("NODES")?.Split(',')?.ToList() ?? [];
var node = new Node(nodes);
builder.Services.AddSingleton<Node>(serviceProvider =>
{
    return node;
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();
app.Run();
