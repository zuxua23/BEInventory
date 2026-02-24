using Microsoft.IdentityModel.Tokens;
using InventoryControl.Entity;
using StackExchange.Redis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenHelper
{
    private readonly IConfiguration _config;

    private readonly IConnectionMultiplexer _redis;

    public JwtTokenHelper(IConfiguration config, IConnectionMultiplexer redis)
    {
        _config = config;
        _redis = redis;
    }

  public async Task<string> GenerateTokenAsync(
    User user,
    List<string> permissions,
    List<string> roles,
    int expireMinutes = 30)
{
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    foreach (var role in roles.Distinct())
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    // Tambahkan permission ke claim
    foreach (var permission in permissions.Distinct())
    {
        claims.Add(new Claim("permission", permission));
    }

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
        Issuer = _config["Jwt:Issuer"],
        Audience = _config["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha256)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    var jwt = tokenHandler.WriteToken(token);

    var db = _redis.GetDatabase();

    await db.StringSetAsync(
        $"jwt:{user.UserId}",
        jwt,
        TimeSpan.FromMinutes(expireMinutes)
    );

    return jwt;
}
}

