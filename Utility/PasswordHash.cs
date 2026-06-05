using System.Security.Cryptography;

namespace InventoryControl.Utility;


public class PasswordHash
{
    private readonly string _salt;

    public PasswordHash()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // pastikan ini di folder root project
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        _salt = configuration["Security:Salt"] ?? throw new ArgumentNullException("Salt is missing in appsettings.json");
    }
    public string HashPassword(string password)
    {
        var combined = password + _salt;
        using var sha = SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(combined);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash); // return hex string
    }

    public bool VerifyPassword(string password, string hash)
    {
        var hashedInput = HashPassword(password);
        return hashedInput.Equals(hash, StringComparison.OrdinalIgnoreCase);
    }
}