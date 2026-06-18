namespace InventoryControl.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using System.Text.Json;
//FOR BE
public class AuthorizePermissionHybridAttribute : Attribute, IAuthorizationFilter
{
    private readonly string? _permission;

    public AuthorizePermissionHybridAttribute(string? permission = null)
    {
        _permission = permission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;
        List<string> userPermissions = new();

        var user = httpContext.User;

        var isLogin = httpContext.Session.GetString("is_login");

        if (isLogin == "OK")
        {
            var permissionsJson = httpContext.Session.GetString("Permissions");

            if (string.IsNullOrEmpty(permissionsJson))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            userPermissions = JsonSerializer.Deserialize<List<string>>(permissionsJson);

        }
        else if (user.Identity != null && user.Identity.IsAuthenticated)
        {
            userPermissions = user.Claims
                .Where(c => c.Type == "permission")
                .Select(c => c.Value)
                .ToList();

        }
        else
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var permissionToCheck = _permission ?? GeneratePermission(context);

        if (!userPermissions.Contains(permissionToCheck))
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