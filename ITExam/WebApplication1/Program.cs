using Hangfire;
using ITExam.Hubs;
using ITExam.Models;
using ITExam.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RoutingAPI>(builder.Configuration.GetSection("RoutingApi"));

builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ITExamDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("SQL"));
});
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ITExam_";
}); 

builder.Services.AddHttpClient();
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 512 * 1024;
    options.EnableDetailedErrors = true;
}); 

builder.Services.AddHostedService<ITExam.Services.FileCleanupService>();
builder.Services.AddTransient<EmailService>();

builder.Services.AddHangfire(x =>
{
    x.UseSqlServerStorage(builder.Configuration.GetConnectionString("SQL"));
});
builder.Services.AddHangfireServer();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddScoped<RedisService>();
builder.Services.AddScoped<IRedisService, RedisService>();
builder.Services.AddScoped<IClassCodeService, ClassCodeService>();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<RoutingAPI>(sp => sp.GetRequiredService<IOptions<RoutingAPI>>().Value);

builder.Services.AddScoped<GeminiService>();

builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<ISubjectAdminService, SubjectAdminService>();

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? string.Empty;
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? string.Empty;
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    throw new InvalidOperationException("Missing JWT signing key: configure `Jwt:SigningKey`.");
}
if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("Missing JWT issuer: configure `Jwt:Issuer`.");
}
if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("Missing JWT audience: configure `Jwt:Audience`.");
}

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(3600)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue("access_token", out var token) &&
                    !string.IsNullOrWhiteSpace(token))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; 
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ITExamDbContext>();
    db.Database.Migrate();

    await SubjectSampleDataSeeder.SeedIfEmptyAsync(db);
    await AuthSampleDataSeeder.SeedIfEmptyAsync(db);
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<ExamMonitorHub>("/examMonitorHub");
app.UseHangfireDashboard("/hangfire");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();