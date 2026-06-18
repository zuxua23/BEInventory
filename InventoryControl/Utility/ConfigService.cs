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
        try
        {
            SystemLogger.Info(
                $"Updating database configuration. Server='{dto.Server}', Database='{dto.Database}'."
            );
            var json = File.ReadAllText(_filePath);
            dynamic obj = JsonConvert.DeserializeObject(json);

            string newConn =
                $"Server={dto.Server};Database={dto.Database};User Id={dto.UserId};Password={dto.Password};Encrypt=True;TrustServerCertificate=True;";

            obj["ConnectionStrings"]["DefaultConnection"] = newConn;

            string output = JsonConvert.SerializeObject(obj, Formatting.Indented);
            File.WriteAllText(_filePath, output);
            SystemLogger.Info(
                "Application configuration updated successfully."
            );
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "Failed to update application configuration.",
                ex
            );

            throw;
        }
    }
}