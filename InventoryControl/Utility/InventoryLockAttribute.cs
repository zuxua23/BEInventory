using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InventoryControl.Utility;

public class InventoryLockAttribute : TypeFilterAttribute
{
    public InventoryLockAttribute()
        : base(typeof(InventoryLockFilter))
    {
    }
}