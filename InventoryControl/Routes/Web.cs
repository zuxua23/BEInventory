
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing.Charts;
using InventoryControl.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Pipelines.Sockets.Unofficial.Arenas;

namespace InventoryControl.Routes;

public static class Web
{
    public static void Map(WebApplication app)
    {
        RegisterAuthRoutes(app);
        RegisterDashboardRoutes(app);
        RegisterMasterRoutes(app);
        RegisterOperationalRoutes(app);
    }

    private static void RegisterAuthRoutes(WebApplication app)
    {
        app.MapControllerRoute(
            name: "login-page",
            pattern: "/",
            defaults: new
            {
                controller = "Auth",
                action = "Index"
            })
            .AddEndpointFilter(GuestFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "auth-login",
            pattern: "/auth/login",
            defaults: new
            {
                controller = "Auth",
                action = "Login"
            })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "auth-logout",
            pattern: "/auth/logout",
            defaults: new
            {
                controller = "Auth",
                action = "Logout"
            });
    }

    private static void RegisterDashboardRoutes(WebApplication app)
    {
        app.MapControllerRoute(
            name: "dashboard",
            pattern: "/dashboard",
            defaults: new
            {
                controller = "Dashboard",
                action = "Index"
            })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));
    }

    private static void RegisterMasterRoutes(WebApplication app)
    {
        RegisterPageRoute(app, "/item", "Item");
        RegisterPageRoute(app, "/location", "Location");
        RegisterPageRoute(app, "/reader", "Reader");
        RegisterPageRoute(app, "/user", "User");
        RegisterPageRoute(app, "/permission", "Permission");
        RegisterPageRoute(app, "/pickinglist", "PickingList");
    }

    private static void RegisterOperationalRoutes(WebApplication app)
    {
        RegisterPageRoute(app, "/printtag", "PrintTagRegis");
        RegisterPageRoute(app, "/stockOut", "StockOut");
        RegisterPageRoute(app, "/stockTaking", "StockTaking");
        RegisterPageRoute(app, "/TransactionHistory", "TransactionHistory");
    }

    private static void RegisterPageRoute(
        WebApplication app,
        string pattern,
        string controller
    )
    {
        var routeName =
            pattern
                .Replace("/", "")
                .ToLower();

        app.MapControllerRoute(
            name: routeName,
            pattern: pattern,
            defaults: new
            {
                controller = controller,
                action = "Index"
            })
            .AddEndpointFilter(AuthFilter)
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));
    }

    private static async ValueTask<object?> AuthFilter(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        if (
            context.HttpContext
                .Session
                .GetString("is_login") != "OK"
        )
        {
            context.HttpContext
                .Response
                .Redirect("/");

            return Results.Empty;
        }

        return await next(context);
    }

    private static async ValueTask<object?> GuestFilter(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next
    )
    {
        if (
            context.HttpContext
                .Session
                .GetString("is_login") == "OK"
        )
        {
            context.HttpContext
                .Response
                .Redirect("/dashboard");

            return Results.Empty;
        }

        return await next(context);
    }
}