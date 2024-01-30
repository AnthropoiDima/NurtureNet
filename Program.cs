using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Neo4jClient;
using Swashbuckle.AspNetCore.Filters;
using NRedisStack;
using StackExchange.Redis;
using System.Net;
using backend.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("CORS", policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowAnyOrigin();
    });
});


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme{
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
    }
);

builder.Services.AddAuthentication().AddJwtBearer(options =>{
    options.TokenValidationParameters = new TokenValidationParameters{
        ValidateIssuerSigningKey = true,
        ValidateIssuer = false,
        ValidateAudience = false,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration.GetSection("AppSettings:Token").Value!))
    };
});

#pragma warning disable
builder.Services.AddSingleton<IConnectionMultiplexer>(option =>
    ConnectionMultiplexer.Connect(new ConfigurationOptions{
        EndPoints = {builder.Configuration.GetConnectionString("redis")},
        AbortOnConnectFail = false,
        Ssl = false,
        Password = ""
    }));
#pragma warning enable

builder.Services.AddSingleton<IGraphClient>(options => {
    var neo4jClient = new GraphClient(
        builder.Configuration.GetConnectionString("neo4j"),
        builder.Configuration.GetSection("NeoKlijent:korisnik").ToString(),
        builder.Configuration.GetSection("NeoKlijent:lozinka").ToString());
    neo4jClient.ConnectAsync().Wait();
    return neo4jClient;
});

builder.Services.AddSignalR();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("CORS");

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hub");

app.Run();