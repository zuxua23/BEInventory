namespace InventoryControl.Routes;

public static class Api
{
    public static void Map(WebApplication app)
    {
        // ─── Auth ───────────────────────────────────────────────────────────
        app.MapControllerRoute(
            name: "api-ping",
            pattern: "/api/ping",
            defaults: new { controller = "Auth", action = "Ping" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

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

        // ─── Tag ────────────────────────────────────────────────────────────
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
            name: "api-tag-validate-epc",
            pattern: "/api/tag/validate-epc",
            defaults: new { controller = "PrintTagRegis", action = "ValidateEpc" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-register-with-item",
            pattern: "/api/tag/register-with-item",
            defaults: new { controller = "PrintTagRegis", action = "RegisterWithItem" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        // ─── Stock In ───────────────────────────────────────────────────────
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

        // ─── Preparation ────────────────────────────────────────────────────
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
            name: "api-preparation-do-detail",
            pattern: "/api/preparation/do/{id}",
            defaults: new { controller = "StockPreparation", action = "GetDoDetail" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-preparation-bulk",
            pattern: "/api/preparation/bulk",
            defaults: new { controller = "StockPreparation", action = "PrepareBulk" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-preparation-bulk-info",
            pattern: "/api/preparation/bulk-info",
            defaults: new { controller = "StockPreparation", action = "GetTagsInfoBulk" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        // ─── Stock Taking (legacy routes — keep as-is) ──────────────────────
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

        // ─── Stock Taking (api/stock-taking routes — sesuai Android) ────────
        app.MapControllerRoute(
            name: "api-stock-taking-create",
            pattern: "/api/stock-taking",
            defaults: new { controller = "StockTaking", action = "Create" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stock-taking-get",
            pattern: "/api/stock-taking",
            defaults: new { controller = "StockTaking", action = "GetStockData" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-active",
            pattern: "/api/stock-taking/active",
            defaults: new { controller = "StockTaking", action = "GetActive" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-loc",
            pattern: "/api/stock-taking/loc",
            defaults: new { controller = "StockTaking", action = "GetLocData" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-scan",
            pattern: "/api/stock-taking/scan",
            defaults: new { controller = "StockTaking", action = "Scan" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stock-taking-bulk-scan",
            pattern: "/api/stock-taking/scan/bulk",
            defaults: new { controller = "StockTaking", action = "Bulk" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stock-taking-remove",
            pattern: "/api/stock-taking/remove",
            defaults: new { controller = "StockTaking", action = "Remove" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stock-taking-manual",
            pattern: "/api/stock-taking/manual-add",
            defaults: new { controller = "StockTaking", action = "ManualAdd" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        // BARU — validate tag untuk manual add dialog
        app.MapControllerRoute(
            name: "api-stock-taking-validate-tag",
            pattern: "/api/stock-taking/validate-tag",
            defaults: new { controller = "StockTaking", action = "ValidateManualTag" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-finalize",
            pattern: "/api/stock-taking/finalize",
            defaults: new { controller = "StockTaking", action = "Finalize" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stock-taking-compare",
            pattern: "/api/stock-taking/compare/{id}",
            defaults: new { controller = "StockTaking", action = "Compare" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-system",
            pattern: "/api/stock-taking/system/{sttId}",
            defaults: new { controller = "StockTaking", action = "GetSystem" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-tags",
            pattern: "/api/stock-taking/tags/{sttId}",
            defaults: new { controller = "StockTaking", action = "GetSessionTags" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-available-tags",
            pattern: "/api/stock-taking/available-tags/{sttId}",
            defaults: new { controller = "StockTaking", action = "GetAvailableTags" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-progress",
            pattern: "/api/stock-taking/progress/{sttId}",
            defaults: new { controller = "StockTaking", action = "GetProgress" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-operator-submit",
            pattern: "/api/stock-taking/operator-submit",
            defaults: new { controller = "StockTaking", action = "OperatorSubmit" })
            .WithMetadata(new HttpMethodMetadata(new[] { "POST" }));

        app.MapControllerRoute(
            name: "api-stock-taking-export-system-excel",
            pattern: "/api/stock-taking/export/system/excel",
            defaults: new { controller = "StockTaking", action = "ExportSystemExcel" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-export-system-csv",
            pattern: "/api/stock-taking/export/system/csv",
            defaults: new { controller = "StockTaking", action = "ExportSystemCsv" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-export-scan-excel",
            pattern: "/api/stock-taking/export/scan/excel",
            defaults: new { controller = "StockTaking", action = "ExportScanExcel" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        app.MapControllerRoute(
            name: "api-stock-taking-export-scan-csv",
            pattern: "/api/stock-taking/export/scan/csv",
            defaults: new { controller = "StockTaking", action = "ExportScanCsv" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        // ─── Location ───────────────────────────────────────────────────────
        app.MapControllerRoute(
            name: "api-location-get",
            pattern: "/api/location",
            defaults: new { controller = "LocationApi", action = "Get" })
            .WithMetadata(new HttpMethodMetadata(new[] { "GET" }));

        // ─── DO / Picking List ──────────────────────────────────────────────
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

        // ─── Search Item ─────────────────────────────────────────────────────
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