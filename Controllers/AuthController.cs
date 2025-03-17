using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MongoDB.Driver;

[Route("api/[controller]")] //->api/auth/[state]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly MongoDbService _mongoDbService;
    private readonly IConfiguration _configuration;

    public AuthController(MongoDbService mongoDbService, IConfiguration configuration)
    {
        _mongoDbService = mongoDbService;
        _configuration = configuration;
    }
//registration
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        var usersCollection = _mongoDbService.GetUsersCollection();

        if (usersCollection.AsQueryable().Any(u => u.Username == user.Username))
            return BadRequest("Username already exists");

        user.UserPass = HashPassword(user.UserPass);

        await usersCollection.InsertOneAsync(user);
        return Ok("User registered successfully");
    }
//user login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] User user)
    {
        var usersCollection = _mongoDbService.GetUsersCollection();
        var existingUser = await usersCollection.Find(u => u.Username == user.Username).FirstOrDefaultAsync();

        if (existingUser == null || !VerifyPassword(user.UserPass, existingUser.UserPass))
            return Unauthorized("Invalid username or password");

        var token = GenerateJwtToken(existingUser);
        return Ok(new { Token = token });
    }
//password hashing logic
    private string HashPassword(string password)
    {
        return Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: new byte[16],
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));
    }
//verifypassword based on hasedpassword
    private bool VerifyPassword(string inputPassword, string storedHash)
    {
        return HashPassword(inputPassword) == storedHash;
    }
//generaitng jwt token after logging for secure accessing
    private string GenerateJwtToken(User user)
    {
        var claims = new[] 
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT_SECRET environment variable is not set.");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
