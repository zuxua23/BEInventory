using InventoryControl.Database;
using InventoryControl.Database.Seeder;
using InventoryControl.Routes;
using InventoryControl.Utility;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();


#region DATABASE
builder.Services.AddDbContext<AppDBContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));
#endregion


#region DEPENDENCY INJECTION
builder.Services.AddApplicationServices();
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
// Mendaftarkan IHttpContextAccessor agar bisa akses Session
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddScoped<JwtTokenHelper>();
#endregion

#region SESSION CONFIG 
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); 
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
#endregion

#region MVC + API
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
#endregion



var app = builder.Build();

#region SEEDER
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedAccess.Initialize(services);
}
#endregion

#region MIDDLEWARE PIPELINE
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseSession();
app.UseStaticFiles();
app.UseAuthorization();
#endregion

#region ROUTING
Web.Map(app);

Api.Map(app);
#endregion

Console.WriteLine(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.Count);
app.Run();
