using InventoryControl.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace InventoryControl.Utility;

public class InventoryLockFilter
    : IAsyncActionFilter
{
    private readonly AppDBContext _db;

    public InventoryLockFilter(
        AppDBContext db
    )
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next
    )
    {
        try
        {
            var method =
                context.HttpContext
                    .Request
                    .Method;

            if (method == "GET")
            {
                await next();
                return;
            }

            var active =
                await _db.StockTakings
                    .AnyAsync(x =>
                        x.Status == "OPEN"
                    );

            if (active)
            {
                var endpoint =
                    context.HttpContext
                        .Request
                        .Path;

                SystemLogger.Warn(
                    $"Inventory lock detected. " +
                    $"Blocked request '{endpoint}' " +
                    $"with method '{method}'."
                );

                context.Result =
                    new BadRequestObjectResult(
                        new
                        {
                            message =
                                "System is locked during stock taking"
                        });

                return;
            }

            await next();
        }
        catch (Exception ex)
        {
            SystemLogger.Error(
                "An error occurred while validating inventory lock.",
                ex
            );

            throw;
        }
    }
}