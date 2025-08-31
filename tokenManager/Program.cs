using System;
using System.Linq;
using System.Collections.Generic;
					
using System;
using System.Threading.Tasks;

namespace TokenDemo
{
    public class TokenResponse
    {
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string access_token { get; set; }
    }

    public interface ITokenService
    {
        Task<TokenResponse> GetTokenAsync();
    }

    public class DummyTokenService : ITokenService
    {
        private int _counter = 0;
        private DateTime _windowStart;

        public DummyTokenService()
        {
            _windowStart = DateTime.UtcNow;
        }

        public Task<TokenResponse> GetTokenAsync()
        {
            // Eğer 1 saat geçtiyse sayaç resetlenir
            if ((DateTime.UtcNow - _windowStart) >= TimeSpan.FromHours(1))
            {
                _counter = 0;
                _windowStart = DateTime.UtcNow;
            }

            // 5'ten fazla istek varsa hata ver
            if (_counter >= 5)
            {
                throw new Exception("Token isteği limiti aşıldı! Saatte en fazla 5 token alınabilir.");
            }

            _counter++;

            return Task.FromResult(new TokenResponse
            {
                token_type = "Bearer",
                expires_in = 10,
                access_token = $"dummy_token_{_counter}_{Guid.NewGuid()}"
            });
        }
    }

    public class TokenManager
    {
        private readonly ITokenService _tokenService;
        private TokenResponse _cachedToken;
        private DateTime _tokenExpiry;

        public TokenManager(ITokenService tokenService)
        {
            _tokenService = tokenService;
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return _cachedToken.access_token; // cache’den döner
            }

            var token = await _tokenService.GetTokenAsync();
            _cachedToken = token;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(token.expires_in);

            return token.access_token;
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var tokenService = new DummyTokenService();
            var tokenManager = new TokenManager(tokenService);

            try
            {
                // 7 defa token isteyelim, 6. ve 7. istekte farklı token kullanılıcak
                for (int i = 1; i <= 7; i++)
                {
                    var token = await tokenManager.GetAccessTokenAsync();
                    Console.WriteLine($"[{i}] Alınan Token: {token}");
                    await Task.Delay(2000); // 2 sn bekle (expire_in=10 sn olduğundan birkaç kere yeni token alınacak)
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
            }
        }
    }
}