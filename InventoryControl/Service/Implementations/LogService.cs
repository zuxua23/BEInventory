namespace InventoryControl.Service.Implementations;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;

public class LogService : ILogService
{
    private readonly string _auditPath =
        Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Audit");

    public List<ActivityLogDto> GetRecentActivities(int take = 10)
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        var filePath = Path.Combine(_auditPath, $"audit-{date}.log");

        if (!File.Exists(filePath))
            return new List<ActivityLogDto>();

        var lines = File.ReadLines(filePath)
            .Reverse()
            .Take(take)
            .ToList();

        var result = new List<ActivityLogDto>();

        foreach (var line in lines)
        {
            try
            {
                var dto = new ActivityLogDto();

                var rawTime = line.Substring(0, 23);
                dto.Time = DateTime.Parse(rawTime).ToString("HH:mm:ss");
                dto.Action = Extract(line, "Action='", "'");
                dto.Entity = Extract(line, "Entity='", "'");
                dto.EntityId = Extract(line, "EntityId='", "'");

                dto.User = Extract(line, "PerformedBy='", "'");

                dto.Description = ExtractToEnd(line, "Description='");

                dto.Source = dto.Action == "LOGIN_ANDROID" ? "Android" :
                             dto.Action == "LOGIN" ? "Web" : null;

                result.Add(dto);

            }
            catch
            {
                // ignore broken line
            }
        }

        return result;
    }

    private string Extract(string source, string start, string end)
    {
        var startIndex = source.IndexOf(start);
        if (startIndex == -1) return "-";
        startIndex += start.Length;
        var endIndex = source.IndexOf(end, startIndex);
        if (endIndex == -1) return "-";
        return source.Substring(startIndex, endIndex - startIndex);
    }

    private string ExtractToEnd(string source, string start)
    {
        var startIndex = source.IndexOf(start);
        if (startIndex == -1) return "-";
        startIndex += start.Length;
        var value = source.Substring(startIndex).TrimEnd();
        if (value.EndsWith("'"))
            value = value.Substring(0, value.Length - 1);
        return string.IsNullOrWhiteSpace(value) ? "-" : value;
    }

}
