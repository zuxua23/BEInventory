using InventoryControl.Database;
using InventoryControl.Database.Seeder;
using InventoryControl.Routes;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.Text;
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
builder.Services.AddSingleton<ImpinjReaderService>();
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<AppRestartService>();
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


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Hybrid";
    options.DefaultChallengeScheme = "Hybrid";
})
.AddPolicyScheme("Hybrid", "JWT or Session", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            return "Bearer";

        return "Cookies"; // fallback
    };
})
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
})
.AddCookie("Cookies");

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
//app.UseAuthorization();
//app.MapControllers();
app.UseAuthentication();
app.UseAuthorization();
#endregion

#region ROUTING
Web.Map(app);

Api.Map(app);
#endregion

Console.WriteLine(app.Services.GetRequiredService<EndpointDataSource>().Endpoints.Count);
app.Run();
