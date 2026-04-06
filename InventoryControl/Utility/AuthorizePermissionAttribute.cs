namespace InventoryControl.Utility;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

public class AuthorizePermissionAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _permission;

    public AuthorizePermissionAttribute(string? permission = null)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        var isLogin = httpContext.Session.GetString("is_login");

        if (isLogin != "OK")
        {
            return;
        }

        var controller = context.RouteData.Values["controller"]?.ToString();

        if (controller == "Auth")
        {
            return;
        }
        Console.WriteLine("=====================");
        var permissionsJson = httpContext.Session.GetString("Permissions");
Console.WriteLine("Session JSON: " + permissionsJson);

        if (string.IsNullOrEmpty(permissionsJson))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissions = JsonSerializer.Deserialize<List<string>>(permissionsJson);
foreach (var p in permissions)
{
    Console.WriteLine("User Permission: " + p);
}
        var permissionToCheck = _permission ?? GeneratePermission(context);
        Console.WriteLine("Required Permission: " + permissionToCheck);

        if (!permissions.Contains(permissionToCheck))
        {
            context.Result = new ObjectResult(new
            {
                status = 403,
                message = "Anda tidak memiliki akses"
            })
            {
                StatusCode = 403
            };
        }
    }

    private string GeneratePermission(AuthorizationFilterContext context)
    {
        var controllerName = context.RouteData.Values["controller"]?.ToString();
        controllerName = controllerName?.Replace("Api", "");

        var method = context.HttpContext.Request.Method;

        var action = method switch
        {
            "POST" => "CREATE",
            "PUT" => "UPDATE",
            "DELETE" => "DELETE",
            "GET" => "GET",
            _ => context.RouteData.Values["action"]?.ToString()
        };

        return $"{controllerName}_{action}".ToUpper();
    }
}