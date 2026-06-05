namespace InventoryControl.Utility;

using InventoryControl.DTO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
    public string GetPrinterName()
    {
        var json = File.ReadAllText(_filePath);
        dynamic obj = JsonConvert.DeserializeObject(json);

        return obj["PrinterSettings"]?["PrinterName"]?.ToString() ?? "";
    }

    public void UpdatePrinterName(string printerName)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(printerName))
                throw new Exception("Printer name cannot be empty.");

            SystemLogger.Info(
                $"Updating printer configuration. PrinterName='{printerName}'."
            );

            var json = File.ReadAllText(_filePath);
            dynamic obj = JsonConvert.DeserializeObject(json);

            if (obj["PrinterSettings"] == null)
            {
                obj["PrinterSettings"] = new Newtonsoft.Json.Linq.JObject();
            }

            obj["PrinterSettings"]["PrinterName"] = printerName;

            string output = JsonConvert.SerializeObject(
                obj,
                Formatting.Indented
            );

            File.WriteAllText(_filePath, output);

            SystemLogger.Info(
                "Printer configuration updated successfully."
            );
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "Failed to update printer configuration.",
                ex
            );

            throw;
        }
    }

    public RfidAdvancedSettingDto GetRfidAdvancedSetting()
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            var obj = JObject.Parse(json);

            var rfid = obj["RFID"];

            if (rfid == null)
            {
                throw new Exception("RFID configuration not found.");
            }

            return new RfidAdvancedSettingDto
            {
                CacheTTLSeconds = rfid["CacheTTLSeconds"]!.Value<int>(),
                ConnectionRetry = rfid["ConnectionRetry"]!.Value<int>(),
                RetryDelayMs = rfid["RetryDelayMs"]!.Value<int>()
            };
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "Failed to get RFID advanced setting.",
                ex
            );

            throw;
        }
    }

    public void UpdateRfidAdvancedSetting(RfidAdvancedSettingDto dto)
    {
        try
        {
            if (dto.CacheTTLSeconds <= 0)
                throw new Exception("Cache TTL must be greater than 0.");

            if (dto.ConnectionRetry <= 0)
                throw new Exception("Connection retry must be greater than 0.");

            if (dto.RetryDelayMs <= 0)
                throw new Exception("Retry delay must be greater than 0.");

            var json = File.ReadAllText(_filePath);
            var obj = JObject.Parse(json);

            if (obj["RFID"] == null)
            {
                obj["RFID"] = new JObject();
            }

            obj["RFID"]["CacheTTLSeconds"] = dto.CacheTTLSeconds;
            obj["RFID"]["ConnectionRetry"] = dto.ConnectionRetry;
            obj["RFID"]["RetryDelayMs"] = dto.RetryDelayMs;

            File.WriteAllText(
                _filePath,
                obj.ToString(Formatting.Indented)
            );

            SystemLogger.Info("RFID advanced setting updated successfully.");
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "Failed to update RFID advanced setting.",
                ex
            );

            throw;
        }
    }
}