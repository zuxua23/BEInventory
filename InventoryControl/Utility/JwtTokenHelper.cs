using Microsoft.IdentityModel.Tokens;
using InventoryControl.Entity;
using InventoryControl.Utility;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

public class JwtTokenHelper
{
    private readonly IConfiguration _config;

    public JwtTokenHelper(
        IConfiguration config
    )
    {
        _config = config;
    }

    public Task<string> GenerateTokenAsync(
        User user,
        List<string> permissions,
        List<string> roles,
        int expireMinutes = 720
    )
    {
        try
        {
            SystemLogger.Info($"Starting JWT token generation for UserId '{user.UserId}'.");

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var displayName = string.IsNullOrWhiteSpace(user.Fullname)
                ? user.Username
                : user.Fullname;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("fullname", displayName),  // ADDED
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles.Distinct())
                claims.Add(new Claim(ClaimTypes.Role, role));

            foreach (var permission in permissions.Distinct())
                claims.Add(new Claim("permission", permission));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256
                )
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            SystemLogger.Info(
                $"JWT token generated successfully. " +
                $"UserId='{user.UserId}', Roles='{roles.Count}', " +
                $"Permissions='{permissions.Count}', ExpireMinutes='{expireMinutes}'."
            );

            return Task.FromResult(jwt);
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                $"An error occurred while generating JWT token for UserId '{user.UserId}'.",
                ex
            );
            throw;
        }
    }
}