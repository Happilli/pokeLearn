using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using DotNetEnv;
using System.Text.Json;

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
            ClockSkew = TimeSpan.Zero,
            ValidateAudience = false,
            ValidateIssuer= false
        };

         options.Events = new JwtBearerEvents()
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("Token validated successfully");
            return Task.CompletedTask;
        }
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
builder.Services.AddHttpClient();
var app = builder.Build();

app.UseRouting();
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 401)
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            StatusCode = 401,
            Message = "Unauthorized - Invalid or missing JWT token"
        }));
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run(); //{runs by dotnet run}
