namespace InventoryControl.Utility;

using System.Runtime.CompilerServices;
using System.Text;

public static class DailyFileLogger
{
    private static readonly object _lock =
        new object();

    private static string _baseLogDirectory =
        "D:\\Fauzan\\Project\\API\\ver9\\InventoryControl\\InventoryControl\\Logs\\";
    private static string _appLogDirectory =
        Path.Combine(
            _baseLogDirectory,
            "App"
        );

    private static string _auditLogDirectory =
        Path.Combine(
            _baseLogDirectory,
            "Audit"
        );

    public static void SetLogDirectory(
        string basePath
    )
    {
        _baseLogDirectory = basePath;

        _appLogDirectory =
            Path.Combine(
                _baseLogDirectory,
                "App"
            );

        _auditLogDirectory =
            Path.Combine(
                _baseLogDirectory,
                "Audit"
            );
    }

    public static void Info(
        string message,
        string? user = null,
        [CallerMemberName]
        string memberName = "",

        [CallerFilePath]
        string filePath = "",

        [CallerLineNumber]
        int lineNumber = 0
    )
    {
        Write(
            "INFO",
            message,
            user,
            memberName,
            filePath,
            lineNumber
        );
    }

    public static void Warn(
        string message,
        string? user = null,
        [CallerMemberName]
        string memberName = "",

        [CallerFilePath]
        string filePath = "",

        [CallerLineNumber]
        int lineNumber = 0
    )
    {
        Write(
            "WARN",
            message,
            user,
            memberName,
            filePath,
            lineNumber
        );
    }

    public static void Error(
        string message,
        Exception? ex = null,
        string? user = null,
        [CallerMemberName]
        string memberName = "",

        [CallerFilePath]
        string filePath = "",

        [CallerLineNumber]
        int lineNumber = 0
    )
    {
        var fullMessage =
            ex == null
                ? message
                : $"{message}{Environment.NewLine}{ex}";

        Write(
            "ERROR",
            fullMessage,
            user,
            memberName,
            filePath,
            lineNumber
        );
    }

    public static void Audit(
        string action,
        string entity,
        string entityId,
        string performedBy,
        string? description = null,

        [CallerMemberName]
        string memberName = "",

        [CallerFilePath]
        string filePath = "",

        [CallerLineNumber]
        int lineNumber = 0
    )
    {
        var message =
            $"Action='{action}' | " +
            $"Entity='{entity}' | " +
            $"EntityId='{entityId}' | " +
            $"PerformedBy='{performedBy}'";

        if (
            !string.IsNullOrWhiteSpace(
                description
            )
        )
        {
            message +=
                $" | Description='{description}'";
        }

        Write(
            "AUDIT",
            message,
            performedBy,
            memberName,
            filePath,
            lineNumber
        );
    }

    private static void Write(
        string level,
        string message,
        string? user,
        string memberName,
        string filePath,
        int lineNumber
    )
    {
        try
        {
            var targetDirectory =
                level == "AUDIT"
                    ? _auditLogDirectory
                    : _appLogDirectory;

            if (
                !Directory.Exists(
                    targetDirectory
                )
            )
            {
                Directory.CreateDirectory(
                    targetDirectory
                );
            }

            CleanOldLogs(
                targetDirectory,
                30
            );

            var date =
                DateTime.Now.ToString(
                    "yyyy-MM-dd"
                );

            var fileName =
                level == "AUDIT"
                    ? $"audit-{date}.log"
                    : $"app-{date}.log";

            var fullPath =
                Path.Combine(
                    targetDirectory,
                    fileName
                );

            var sourceInfo =
                $"{Path.GetFileName(filePath)}:" +
                $"{lineNumber} " +
                $"({memberName})";

            var logBuilder =
                new StringBuilder()
                    .Append(
                        DateTime.Now.ToString(
                            "yyyy-MM-dd HH:mm:ss.fff"
                        )
                    )
                    .Append(" [")
                    .Append(level)
                    .Append("] ");

            if (
                !string.IsNullOrWhiteSpace(
                    user
                )
            )
            {
                logBuilder
                    .Append("[User:")
                    .Append(user)
                    .Append("] ");
            }

            logBuilder
                .Append(sourceInfo)
                .Append(" - ")
                .Append(message)
                .AppendLine();

            lock (_lock)
            {
                File.AppendAllText(
                    fullPath,
                    logBuilder.ToString()
                );
            }
        }
        catch
        {
            // Logging should never crash the application
        }
    }

    private static void CleanOldLogs(
        string directoryPath,
        int days
    )
    {
        try
        {
            var directory =
                new DirectoryInfo(
                    directoryPath
                );

            if (!directory.Exists)
            {
                return;
            }

            var files =
                directory.GetFiles("*.log");

            foreach (var file in files)
            {
                if (
                    file.LastWriteTime <
                    DateTime.Now.AddDays(-days)
                )
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        // Ignore file delete errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}