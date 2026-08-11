using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Configuration;
namespace Backend.Controllers
{
    [Route("api/{tenant?}/[controller]")]
    [ApiController]
    public class TelegramController : ControllerBase
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly TgValidationService _tgValidationService;
        private readonly IConfiguration _configuration;

        public TelegramController(IDbContextFactory<AppDbContext> dbContextFactory, TgValidationService tgValidationService, IConfiguration configuration)
        {
            _dbContextFactory = dbContextFactory;
            _tgValidationService = tgValidationService;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> RegisterTMA(string tenant, string initData)
        {
            if(tenant == null)
            {
                return BadRequest("Tenant is required.");
            }

            var id = User.Claims.FirstOrDefault();
            if (id != null) 
            {
                return BadRequest("User is already registered.");
            }


            var context = await _dbContextFactory.CreateDbContextAsync();
            string botToken = context.BarbershopOwners.FirstOrDefault(t => t.Id.ToString() == tenant)?.BotToken ?? string.Empty;
            if (!_tgValidationService.ValidateInitData(initData, botToken))
            {
                return BadRequest("Invalid initialization data.");
            }

            var tgUser = _tgValidationService.GetUserFromInitData(initData);
            if (tgUser == null) return BadRequest("Invalid user data.");
            
            string userId = tgUser.Id.ToString();
            string clientName = !string.IsNullOrWhiteSpace(tgUser.FirstName) 
                ? $"{tgUser.FirstName} {tgUser.LastName}".Trim() 
                : tgUser.Username ?? "Unknown Client";

            var master = await context.Masters.FirstOrDefaultAsync(m => m.TelegramId == userId);

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings.GetValue<string>("Secret");

            if (master == null) 
            {
                var client = context.Clients.FirstOrDefault(c => c.TelegramId == userId);
                string clientId = client?.Id.ToString() ?? string.Empty;

                if (client == null) 
                {
                    clientId = Guid.NewGuid().ToString();
                    Client newClient = new Client
                    {
                        Id = Guid.Parse(clientId),
                        TelegramId = userId,
                        OwnerId = Guid.Parse(tenant),
                        Name = clientName
                    };
                    context.Clients.Add(newClient);
                    await context.SaveChangesAsync();
                    clientId = newClient.Id.ToString();
                }                

                var UserClaims = new[]
                {
                    new Claim("UserId", clientId),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                var ClientKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
                var ClientCreds = new SigningCredentials(ClientKey, SecurityAlgorithms.HmacSha256);

                var ClientToken = new JwtSecurityToken(
                    issuer: jwtSettings.GetValue<string>("Issuer"),
                    audience: jwtSettings.GetValue<string>("Audience"),
                    claims: UserClaims,
                    expires: DateTime.UtcNow.AddDays(7),
                    signingCredentials: ClientCreds
                );

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(ClientToken),
                    expiration = ClientToken.ValidTo,
                    message = "User registered successfully."
                });
            }

            master.TelegramId = userId;
            master.Ip = HttpContext.Connection.RemoteIpAddress;

            await context.SaveChangesAsync();

            var claims = new[]
            {
                new Claim("MasterId", master.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.GetValue<string>("Issuer"),
                audience: jwtSettings.GetValue<string>("Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                message = "User registered successfully."
            });
        }
    }
}
