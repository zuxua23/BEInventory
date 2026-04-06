using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace InventoryControl.Routes;

public static class Web
{
    public static void Map(WebApplication app)
    {
        app.MapControllerRoute(
            name: "default",
            pattern: "/",
            defaults: new { controller = "Auth", action = "Index" })
            .AddEndpointFilter(async (context, next) =>
            {
                if (context.HttpContext.Session.GetString("is_login") == "OK")
                {
                    context.HttpContext.Response.Redirect("/home");
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
            name: "logout",
            pattern: "/auth/logout",
            defaults: new { controller = "Auth", action = "Logout" });

        app.MapControllerRoute(
            name: "home",
            pattern: "auth/home",
            defaults: new { controller = "Home", action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

    //    app.MapControllerRoute(
    //name: "home",
    //pattern: "/home",
    //defaults: new { controller = "Home", action = "Index" })
    //.AddEndpointFilter(async (context, next) =>
    //{
    //    // Jika user sudah login, redirect ke dashboard
    //    if (context.HttpContext.Session.GetString("is_login") != "OK")
    //    {
    //        context.HttpContext.Response.Redirect("/");
    //        return Results.Empty;
    //    }
    //    return await next(context);
    //})
    //.WithMetadata(new HttpMethodMetadata(new[] { "GET" }));


        MapCrud(app, "item", "Item");
        MapCrud(app, "location", "Location");
        MapCrud(app, "reader", "Reader");
        MapCrud(app, "user", "User");

        app.MapControllerRoute("printtag", "/printtag",
            new { controller = "PrintTag", action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute("printtag-print", "/printtag/print",
            new { controller = "PrintTag", action = "Print" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute("printtag-delete", "/printtag/delete",
            new { controller = "PrintTag", action = "Delete" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));


        app.MapControllerRoute("picking", "/picking",
            new { controller = "Picking", action = "Index" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute("picking-create", "/picking",
            new { controller = "Picking", action = "Create" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute("picking-data", "/picking/data",
            new { controller = "Picking", action = "Get" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute("picking-update", "/picking/update",
            new { controller = "Picking", action = "Update" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute("picking-delete", "/picking/delete",
            new { controller = "Picking", action = "Delete" })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));


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
        Console.WriteLine("FILTER: " + context.HttpContext.Request.Path);
        Console.WriteLine("SESSION: " + context.HttpContext.Session.GetString("is_login"));
        if (context.HttpContext.Session.GetString("is_login") != "OK")
        {
            context.HttpContext.Response.Redirect("/");
            return Results.Empty;
        }

        return await next(context);
    }
}