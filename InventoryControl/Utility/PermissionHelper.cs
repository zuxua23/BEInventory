namespace InventoryControl.Utility;


using System.Text.Json;

public static class PermissionHelper
{
    public static bool HasPermission(HttpContext context, string permission)
    {
        var json = context.Session.GetString("Permissions");

        if (string.IsNullOrEmpty(json))
            return false;

        var permissions = JsonSerializer.Deserialize<List<string>>(json);

        return GetPermissions(context).Contains(permission);
    }

    public static bool HasAnyPermission(HttpContext context, params string[] perms)
    {
        var json = context.Session.GetString("Permissions");

        if (string.IsNullOrEmpty(json))
            return false;

        var permissions = GetPermissions(context);
        return perms.Any(p => permissions.Contains(p));
    }
    public static List<string> GetPermissions(HttpContext context)
    {
        if (context.Items["permissions"] is List<string> cached)
            return cached;

        var json = context.Session.GetString("Permissions");

        if (string.IsNullOrEmpty(json))
            return new List<string>();

        var permissions = JsonSerializer.Deserialize<List<string>>(json);

        context.Items["permissions"] = permissions;

        return permissions;
    }
}