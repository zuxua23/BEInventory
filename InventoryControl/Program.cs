using InventoryControl.Consumers;
using InventoryControl.Database;
using InventoryControl.Database.Seeder;
using InventoryControl.Handler;
using InventoryControl.Utility;
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


#region DEPENDENCY INJECTION
builder.Services.AddSingleton<JwtTokenHelper>();
builder.Services.AddScoped<CommandDispatcher>();
builder.Services.AddApplicationServices();
builder.Services.AddCommandHandlers();
builder.Services.AddHostedService<UserConsumer>();
builder.Services.AddHostedService<ItemConsumer>();

#endregion


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
