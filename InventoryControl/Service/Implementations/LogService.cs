namespace InventoryControl.Service.Implementations;

using InventoryControl.DTO;
using InventoryControl.Service.Interfaces;

public class LogService : ILogService
{
    private readonly string _auditPath =
        Path.Combine(
            Directory.GetCurrentDirectory(),
            "Logs",
            "Audit"
        );

    public List<ActivityLogDto>
        GetRecentActivities(int take = 10)
    {
        var date =
            DateTime.Now.ToString("yyyy-MM-dd");

        var filePath =
            Path.Combine(
                _auditPath,
                $"audit-{date}.log"
            );

        if (!File.Exists(filePath))
        {
            return new List<ActivityLogDto>();
        }

        var lines = File.ReadAllLines(filePath)
            .Reverse()
            .Take(take)
            .ToList();

        var result =
            new List<ActivityLogDto>();

        foreach (var line in lines)
        {
            try
            {
                var dto =
                    new ActivityLogDto();

                dto.Time =
                    line.Substring(0, 23);

                dto.Action =
                    Extract(line, "Action='", "'");

                dto.Entity =
                    Extract(line, "Entity='", "'");

                dto.User =
                    Extract(line, "PerformedBy='", "'");

                result.Add(dto);
            }
            catch
            {
                // ignore broken line
            }
        }

        return result;
    }

    private string Extract(
        string source,
        string start,
        string end
    )
    {
        var startIndex =
            source.IndexOf(start);

        if (startIndex == -1)
        {
            return "-";
        }

        startIndex += start.Length;

        var endIndex =
            source.IndexOf(
                end,
                startIndex
            );

        if (endIndex == -1)
        {
            return "-";
        }

        return source.Substring(
            startIndex,
            endIndex - startIndex
        );
    }
}