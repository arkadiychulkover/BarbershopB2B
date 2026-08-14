using Backend.Data;
using Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Backend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                });

            // CORS configuration for TMA and Web frontends
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
                
            builder.Services.AddScoped<AppDbContext>(p => 
                p.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<StatisticService>();
            builder.Services.AddScoped<TonService>();
            builder.Services.AddScoped<TgValidationService>();
            builder.Services.AddSingleton<BotService>();

            builder.Services.AddHostedService<Backend.BackgroundServices.NotifycationBackgroundService>();
            builder.Services.AddHostedService<Backend.BackgroundServices.SubscriptionBackgroundService>();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("Secret") ?? "DefaultSecretKeyForDevelopmentOnlyAtLeast32BytesLong!";

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
                    ValidAudience = jwtSettings.GetValue<string>("Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });

            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
            });

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler(handler => handler.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("{\"error\":\"Internal Server Error\"}");
                }));
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.MapControllers();

            app.Run();
        }
    }
}
