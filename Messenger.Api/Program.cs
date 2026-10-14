using Messenger.Api.Middleware;
using Messenger.Infrastructure;
using Messenger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Messenger.Application;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Messenger.Infrastructure.Security;
using System.Text.Json.Serialization;
using Messenger.Application.Conversations.Interfaces;
using Messenger.Api.Services;
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000") // Đặt đúng port của React Vite
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Yêu cầu tự động convert chuỗi (VD: "TEXT") thành Enum tương ứng
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddApplication(); // them các dịch vụ của tầng Application vào DI container
builder.Services.AddInfrastructure(builder.Configuration); // them các dịch vụ của tầng Infrastructure vào DI container
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Nhập JWT Token của bạn theo dạng: Bearer {your_token}"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// Lấy cấu hình JwtSettings từ appsettings.json
var jwtSettingsSection = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettingsSection!.Issuer,
        ValidAudience = jwtSettingsSection!.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSettingsSection.SecretKey))
    };

    options.Events = new JwtBearerEvents
    {
        // xử lý sự kiện OnMessageReceived để lấy token từ query string khi kết nối đến signalR hub
        
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            // nếu req gửi đến endpoint của signalR và có token
            if(!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
            {
               context.Token = accessToken ; 
            }

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddSignalR();
builder.Services.AddScoped<IChatNotificationService, ChatNotificationService>();
var app = builder.Build();
//test db connection 
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    var canConnect = dbContext.Database.CanConnect();

    Console.WriteLine(
        canConnect
            ? "Database connection successful."
            : "Database connection failed.");
}

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("CorsPolicy");

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<Messenger.Api.Hubs.ChatHub>("/hubs/chat");

app.MapControllers();

// Seed dữ liệu mẫu khi ứng dụng khởi động
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbcontext = services.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.SeedAsync(dbcontext);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
