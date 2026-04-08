
namespace InventoryControl.Routes;

public static class Web
{
    public static void Map(WebApplication app)
    {
        Console.WriteLine("Web.Map executed");
        app.MapControllerRoute(
            name: "dashboard",
            pattern: "/dashboard",
            defaults: new { controller = "Dashboard", action = "Index" })
            .AddEndpointFilter(async (context, next) =>
            {
                // Jika user sudah login, redirect ke dashboard
                if (context.HttpContext.Session.GetString("is_login") != "OK")
                {
                    context.HttpContext.Response.Redirect("/");
                    return Results.Empty;
                }
                return await next(context);
            })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "default",
            pattern: "/",
            defaults: new { controller = "Auth", action = "Index" })
            .AddEndpointFilter(async (context, next) =>
            {
                if (context.HttpContext.Session.GetString("is_login") == "OK")
                {
                    context.HttpContext.Response.Redirect("/dashboard");
                    return Results.Empty;
                }

                return await next(context);
            })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "auth-login",
            pattern: "/auth/login",
            defaults: new { controller = "Auth", action = "Login" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "auth-logout",
            pattern: "/auth/logout",
            defaults: new { controller = "Auth", action = "Logout" });

        MapCrud(app, "item", "Item");
        MapCrud(app, "location", "Location");
        MapCrud(app, "reader", "Reader");
        MapCrud(app, "user", "User");

        app.MapControllerRoute("printtag", "/printtag",
            new { controller = "PrintTagRegis", action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute("printtag-print", "/printtag/print",
            new { controller = "PrintTagRegis", action = "Print" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
    name: "stockOut",
    pattern: "/stockOut",
    defaults: new { controller = "StockOut", action = "Index" })
            .AddEndpointFilter(AuthFilter)
    .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));


        app.MapControllerRoute("pickinglist", "/pickinglist",
            new { controller = "Pickinglist", action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute("picking-create", "/pickinglist",
            new { controller = "PickingList", action = "Create" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        //app.MapControllerRoute("picking-data", "/pickinglist/data",
        //    new { controller = "PickingList", action = "Get" })
        //    .AddEndpointFilter(AuthFilter)
        //    .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        //app.MapControllerRoute("picking-update", "/picking/update",
        //    new { controller = "Picking", action = "Update" })
        //    .AddEndpointFilter(AuthFilter)
        //    .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        //app.MapControllerRoute("picking-delete", "/picking/delete",
        //    new { controller = "Picking", action = "Delete" })
        //    .AddEndpointFilter(AuthFilter)
        //    .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));


        app.MapControllerRoute("permission", "/permission",
            new { controller = "Permission", action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute("permission-update", "/permission/update",
            new { controller = "Permission", action = "Update" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute("permission-delete", "/permission/delete",
            new { controller = "Permission", action = "Delete" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));
    }


    private static void MapCrud(WebApplication app, string route, string controller)
    {
        app.MapControllerRoute($"{route}",
            $"/{route}",
            new { controller = controller, action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute($"{route}-create",
            $"/{route}",
            new { controller = controller, action = "Create" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute($"{route}-data",
            $"/{route}/data",
            new { controller = controller, action = "Get" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute($"{route}-update",
            $"/{route}/update",
            new { controller = controller, action = "Update" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute($"{route}-delete-form",
            $"/{route}/delete-form",
            new { controller = controller, action = "DeleteForm" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute($"{route}-delete",
            $"/{route}/delete",
            new { controller = controller, action = "Delete" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));
    }


    private static async ValueTask<object?> AuthFilter(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (context.HttpContext.Session.GetString("is_login") != "OK")
        {
            context.HttpContext.Response.Redirect("/");
            return Results.Empty;
        }

        return await next(context);
    }
}