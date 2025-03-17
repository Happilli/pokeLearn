using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
//loadin .env [using .env instead of appseeting.json for security and other stuff]
Env.Load();


//jwt authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddSingleton<IMongoClient>(serviceProvider =>
{
    var connectionString = Environment.GetEnvironmentVariable("MONGO_DB_CONNECTION");
    return new MongoClient(connectionString);
});

//adding modular files needed
builder.Services.AddSingleton<MongoDbService>();
builder.Services.AddControllers();
var app = builder.Build();
app.UseAuthentication();
app.UseRouting();
app.MapControllers();


app.Run();
