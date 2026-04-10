namespace InventoryControl.Routes;

public static class Api
{
    public static void Map(WebApplication app)
    {
        app.MapControllerRoute(
            name: "api-login",
            pattern: "/api/auth/login",
            defaults: new { controller = "Auth", action = "LoginHT" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-logout",
            pattern: "/api/auth/logout",
            defaults: new { controller = "Auth", action = "LogoutHT" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-register",
            pattern: "/api/tag/register",
            defaults: new { controller = "PrintTagRegis", action = "Register" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stockin",
            pattern: "/api/stockin",
            defaults: new { controller = "StockIn", action = "StockIn" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-preparation",
            pattern: "/api/preparation",
            defaults: new { controller = "StockPreparation", action = "Prepare" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));
    }
}