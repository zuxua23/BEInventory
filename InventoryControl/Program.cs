using InventoryControl.Database;
using InventoryControl.Database.Seeder;
using InventoryControl.Entity;
using InventoryControl.Services.Implementations;
using InventoryControl.Services.Interfaces;
using InventoryControl.Utility;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

using Serilog;
using Serilog.Enrichers.CallerInfo;
using Serilog.Events;
using StackExchange.Redis;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog();

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
builder.Services.AddScoped<JwtTokenHelper>();



#region MVC + API
builder.Services.AddControllersWithViews();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});
#endregion

#region SESSION (WEB ONLY)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
#endregion




#region AUTHENTICATION
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
        ),

        };

        //  JWT CUSTOM ERROR (API ONLY)
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                    return Task.CompletedTask;

                if (context.Response.HasStarted)
                    return Task.CompletedTask;

                context.HandleResponse();

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var result = JsonConvert.SerializeObject(new
                {
                    status = 401,
                    code = "TOKEN_REQUIRED",
                    message = "Token tidak ditemukan"
                });

                return context.Response.WriteAsync(result);
            },

            OnAuthenticationFailed = context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                    return Task.CompletedTask;

                if (context.Response.HasStarted)
                    return Task.CompletedTask;

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var isExpired =
                    context.Exception is SecurityTokenExpiredException;

                var result = JsonConvert.SerializeObject(new
                {
                    status = 401,
                    code = isExpired ? "TOKEN_EXPIRED" : "TOKEN_INVALID",
                    message = isExpired
                        ? "Token sudah kadaluarsa"
                        : "Token tidak valid"
                });

                return context.Response.WriteAsync(result);
            },
                OnTokenValidated = async context =>
                {
                    var redis = context.HttpContext.RequestServices
                        .GetRequiredService<IConnectionMultiplexer>();

                    var principal = context.Principal;

                    var userId = principal?
                        .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    var username = principal?
                        .FindFirst(ClaimTypes.Name)?.Value;

                    var roles = principal?
                        .FindAll(ClaimTypes.Role)
                        .Select(r => r.Value)
                        .ToList();

                    var permissions = principal?
                        .FindAll("permission")
                        .Select(p => p.Value)
                        .ToList();

                    Console.WriteLine("===== JWT TOKEN VALIDATED =====");
                    Console.WriteLine($"UserId     : {userId}");
                    Console.WriteLine($"Username   : {username}");
                    Console.WriteLine($"Roles      : {string.Join(", ", roles ?? new List<string>())}");
                    Console.WriteLine($"Permissions: {string.Join(", ", permissions ?? new List<string>())}");



                    var authHeader = context.Request.Headers["Authorization"]
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(authHeader) ||
                        !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Authorization header tidak valid");
                        context.Fail("Authorization header tidak valid");
                        return;
                    }

                    var rawToken = authHeader
                        .Substring("Bearer ".Length)
                        .Trim();

                    var db = redis.GetDatabase();
                    var storedToken = await db.StringGetAsync($"jwt:{userId}");

                    Console.WriteLine("===== REDIS CHECK =====");
                    Console.WriteLine($"Redis Key  : jwt:{userId}");
                    Console.WriteLine($"Stored JWT : {storedToken}");
                    Console.WriteLine($"Incoming   : {rawToken}");

                    if (storedToken.IsNullOrEmpty)
                    {
                        Console.WriteLine("Token tidak ditemukan di Redis");
                        context.Fail("Token tidak valid / sudah logout");
                        return;
                    }

                    if (storedToken != rawToken)
                    {
                        Console.WriteLine("Token tidak cocok dengan Redis");
                        context.Fail("Token mismatch");
                        return;
                    }
                    Console.WriteLine("===== ALL CLAIMS =====");
                    foreach (var claim in principal.Claims)
                    {
                        Console.WriteLine($"Type: {claim.Type} | Value: {claim.Value}");
                    }
                    Console.WriteLine("JWT VALID & REDIS MATCH ✅");
                }
            


        };

    });
#endregion

#region AUTHORIZATION (ROLE + PERMISSION)
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("USER_VIEW",
        policy => policy.RequireClaim("permission", "USER_VIEW"));

    options.AddPolicy(PermissionPolicies.MasterUserCreate,
        policy => policy.RequireClaim("permission", "MASTER_USER_CREATE"));

    options.AddPolicy("USER_UPDATE",
        policy => policy.RequireClaim("permission", "USER_UPDATE"));

    options.AddPolicy("USER_DELETE",
        policy => policy.RequireClaim("permission", "USER_DELETE"));
});
#endregion

#region DEPENDENCY INJECTION
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();

#endregion

var app = builder.Build();

#region MIDDLEWARE PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedAccess.Initialize(services);
}
app.UseHttpsRedirection();

app.UseRouting();

app.UseSession();          // WEB
app.UseAuthentication();   // JWT
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
// WEB MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

// API
app.MapControllers();
#endregion



//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDBContext>();

//    if (!db.Users.Any())
//    {
//        db.Users.Add(new User
//        {
//            Id = Guid.NewGuid().ToString(),
//            UserId = "USR001",
//            Fullname = "Administrator",
//            Username = "admin",
//            Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
//            CreatedAt = DateTime.Now,
//            CreatedBy = "SYSTEM",
//            IsDelete = 0
//        });

//        db.SaveChanges();
//    }
//}



app.Run();
