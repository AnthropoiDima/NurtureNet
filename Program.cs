using Neo4jClient;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IGraphClient>(options => {
    var neo4jClient = new GraphClient(
        builder.Configuration.GetConnectionString("neo4j"),
        builder.Configuration.GetSection("NeoKlijent:korisnik").ToString(),
        builder.Configuration.GetSection("NeoKlijent:lozinka").ToString());
    neo4jClient.ConnectAsync().Wait();
    return neo4jClient;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
