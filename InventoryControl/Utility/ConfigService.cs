namespace InventoryControl.Utility;

using InventoryControl.DTO;
using Newtonsoft.Json;

public class ConfigService
{
    private readonly string _filePath;

    public ConfigService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "appsettings.json");
    }

    public void UpdateConnection(UpdateConnectionDto dto)
    {
        var json = File.ReadAllText(_filePath);
        dynamic obj = JsonConvert.DeserializeObject(json);

        string newConn =
            $"Server={dto.Server};Database={dto.Database};User Id={dto.UserId};Password={dto.Password};Encrypt=True;TrustServerCertificate=True;";

        obj["ConnectionStrings"]["DefaultConnection"] = newConn;

        string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
        File.WriteAllText(_filePath, output);
    }
}