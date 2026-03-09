using InventoryControl.Consumers;
using InventoryControl.Database;
using InventoryControl.Database.Seeder;
using InventoryControl.Handler;
using InventoryControl.Service.Implementations;
using InventoryControl.Service.Interfaces;
using InventoryControl.Services.Implementations;
using InventoryControl.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(
        builder.Configuration["Redis:Connection"]
    )
);

#region DATABASE
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion

builder.Services.AddHostedService<RedisConsumer>();
builder.Services.AddSingleton<JwtTokenHelper>();
builder.Services.AddScoped<CommandDispatcher>();
#region MVC + API
//builder.Services.AddControllersWithViews();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    //options.JsonSerializerOptions.WriteIndented = true;
});
#endregion


//#region AUTHORIZATION (ROLE + PERMISSION)
//builder.Services.AddAuthorization();

//builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();

//builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
//#endregion

#region DEPENDENCY INJECTION
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IItemService, ItemService>();
builder.Services.AddScoped<IReaderService, ReaderService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStockInService, StockInService>();
builder.Services.AddScoped<IStockOutService, StockOutService>();
builder.Services.AddScoped<IStockPreparationService, StockPreparationService>();
builder.Services.AddScoped<IReaderService, ReaderService>();
builder.Services.AddScoped<IDOService, DOService>();
builder.Services.AddScoped<IStockTakingService, StockTakingService>();
builder.Services.AddScoped<IPrintTagRegisService, PrintTagRegisService>();
builder.Services.AddScoped<UserHandler>();
#endregion

var app = builder.Build();

#region MIDDLEWARE PIPELINE
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedAccess.Initialize(services);
}
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();
#endregion

#region API FORBIDDEN (403) CUSTOM
app.Use(async (context, next) =>
{
    await next();

    if (context.Response.StatusCode == 403 &&
        context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.ContentType = "application/json";

        var result = JsonConvert.SerializeObject(new
        {
            status = 403,
            code = "FORBIDDEN",
            message = "Anda tidak memiliki akses"
        });

        await context.Response.WriteAsync(result);
    }
});
#endregion

#region ROUTING
// API
app.MapControllers();
#endregion

app.Run();
