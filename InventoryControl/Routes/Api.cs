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
            name: "api-print",
            pattern: "/api/tag/print",
            defaults: new { controller = "PrintTagRegis", action = "Print" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stockin",
            pattern: "/api/stockin",
            defaults: new { controller = "StockIn", action = "StockIn" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stockin-gettag",
            pattern: "/api/stockin/{code}",
            defaults: new { controller = "StockIn", action = "GetTagByCode" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-preparation",
            pattern: "/api/preparation",
            defaults: new { controller = "StockPreparation", action = "Prepare" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-preparation-do",
            pattern: "/api/preparation/do",
            defaults: new { controller = "StockPreparation", action = "GetDoDrafts" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stocktaking-create",
            pattern: "/api/stocktaking/create",
            defaults: new { controller = "StockTaking", action = "Create" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stocktaking-getdata",
            pattern: "/api/stocktaking/data",
            defaults: new { controller = "StockTaking", action = "GetStockData" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stocktaking-scan",
            pattern: "/api/stocktaking/scan",
            defaults: new { controller = "StockTaking", action = "Scan" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stocktaking-remove",
            pattern: "/api/stocktaking/remove",
            defaults: new { controller = "StockTaking", action = "Remove" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stocktaking-manual",
            pattern: "/api/stocktaking/manual-add",
            defaults: new { controller = "StockTaking", action = "ManualAdd" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stocktaking-finalize",
            pattern: "/api/stocktaking/finalize",
            defaults: new { controller = "StockTaking", action = "Finalize" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-location-get",
            pattern: "/api/location",
            defaults: new { controller = "LocationApi", action = "Get" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-do-list",
            pattern: "/api/do",
            defaults: new { controller = "PickingListApi", action = "Get" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-pickinglist-list",
            pattern: "/api/pickinglist",
            defaults: new { controller = "PickingListApi", action = "Get" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-pickinglist-detail",
            pattern: "/api/pickinglist/{id}",
            defaults: new { controller = "PickingListApi", action = "GetById" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-preparation-bulk",
            pattern: "/api/preparation/bulk",
            defaults: new { controller = "StockPreparation", action = "PrepareBulk" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-search-item-list",
            pattern: "/api/search-item",
            defaults: new { controller = "SearchItem", action = "GetAll" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-search-item-detail",
            pattern: "/api/search-item/{code}",
            defaults: new { controller = "SearchItem", action = "GetDetail" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));
    }
}