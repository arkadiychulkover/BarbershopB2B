using Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Backend.Services
{
    public class TonService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _address;


        public TonService(IDbContextFactory<AppDbContext> contextFactory, IHttpClientFactory httpFactory, IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _httpFactory = httpFactory;
            _configuration = configuration;

            _apiKey = _configuration["Ton:ApiKey"];
            _address = _configuration["Ton:Address"];
        }

        public async Task<bool> CheckTranzaction(string txhHash, decimal amount)
        {
            try
            {
                var bocBytes = Convert.FromBase64String(txhHash.Replace('-', '+').Replace('_', '/'));
                var expectedHashHex = Convert.ToHexString(SHA256.Create().ComputeHash(bocBytes)).ToLower();

                using var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                client.DefaultRequestHeaders.Add("User-Agent", "BarbershopB2B/1.0");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var response = await client.GetAsync($"https://tonapi.io/v2/blockchain/accounts/{_address}/transactions?limit=20");
                if (!response.IsSuccessStatusCode) return false;

                var rawBody = await response.Content.ReadAsStringAsync();
                var content = System.Text.Json.JsonDocument.Parse(rawBody);
                
                if (!content.RootElement.TryGetProperty("transactions", out var transactions) || 
                    transactions.ValueKind != System.Text.Json.JsonValueKind.Array) return false;

                long tenMinutesAgo = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 600;
                using var checkContext = await _contextFactory.CreateDbContextAsync();

                foreach (var tx in transactions.EnumerateArray())
                {
                    if (tx.GetProperty("utime").GetInt64() < tenMinutesAgo) continue;
                    
                    var txHash = tx.GetProperty("hash").GetString();
                    if (txHash?.ToLower() != expectedHashHex) continue;

                    if (!tx.TryGetProperty("in_msg", out var msg) || msg.GetProperty("msg_type").GetString() != "int_msg") continue;

                    string destination = msg.TryGetProperty("destination", out var dst) && dst.TryGetProperty("address", out var addr) 
                        ? addr.GetString() : null;
                    
                    decimal txAmount = msg.TryGetProperty("value", out var val) ? val.GetInt64() / 1_000_000_000m : 0;

                    if (destination == _address && txAmount >= amount)
                    {
                        if (await checkContext.Tranxactions.AnyAsync(t => t.TxhHash == txhHash || t.TxhHash == txHash)) return false;
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }
    }
}
